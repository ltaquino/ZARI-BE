namespace ZARI.Application.Features.Loan.Reports.CicCsdf;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.Reports.CicCsdf.Shared;
using ZARI.Application.Features.Loan.Reports.CisaCreditData;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Builds the `ID`+`CI` record data for a CIC CSDF v1.4 submission file
/// (ZARI-FE/frs/loan-cic/LoanCicContext.md). Reuses
/// <see cref="GetCisaCreditDataExportQueryHandler"/>'s own derived outstanding-balance/days-overdue/
/// restructured/write-off computation as a building block (context doc §3) instead of recomputing
/// it, so the CISA extract and this real submission file never silently disagree on what "current
/// balance" or "past due" means for the same account.
/// </summary>
public sealed class GetCicCsdfExportQueryHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    IQueryHandler<GetCisaCreditDataExportQuery, Result<List<CisaCreditDataRow>>> cisaHandler)
    : IQueryHandler<GetCicCsdfExportQuery, Result<CicCsdfExport>>
{
    // Obviously-fake placeholder so an unconfigured environment produces an obviously-wrong file
    // rather than a silently wrong one — see GetCicCsdfExportQuery.cs's doc comment on ProviderCode.
    private const string PlaceholderProviderCode = "UNSET000";

    public async Task<Result<CicCsdfExport>> HandleAsync(GetCicCsdfExportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<CicCsdfExport>(Error.Forbidden("CicCsdf.Forbidden", "You do not have permission to view loan accounts."));

        var providerCode = string.IsNullOrWhiteSpace(query.ProviderCode) ? PlaceholderProviderCode : query.ProviderCode;
        var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;

        var accountsQuery = dbContext.LoanAccounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.ScheduleLines)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.BranchId)) accountsQuery = accountsQuery.Where(a => a.BranchId == query.BranchId);
        if (query.CustomerId is { } customerId) accountsQuery = accountsQuery.Where(a => a.CustomerId == customerId);

        var accounts = await accountsQuery.ToListAsync(cancellationToken);
        if (accounts.Count == 0) return Result.Success(new CicCsdfExport(providerCode, asOfDate, [], []));

        var cisaResult = await cisaHandler.HandleAsync(new GetCisaCreditDataExportQuery(query.BranchId, query.CustomerId, asOfDate), cancellationToken);
        if (cisaResult.IsFailure) return Result.Failure<CicCsdfExport>(cisaResult.Error!);
        var cisaByAccountId = cisaResult.Value!.ToDictionary(r => r.LoanAccountId);

        var applicationIds = accounts.Where(a => a.LoanApplicationId is not null).Select(a => a.LoanApplicationId!.Value).Distinct().ToList();
        var coMakersByApplicationId = applicationIds.Count == 0
            ? new Dictionary<Guid, List<LoanCoMaker>>()
            : (await dbContext.LoanCoMakers.AsNoTracking()
                .Include(cm => cm.CoMakerCustomer)
                .Where(cm => applicationIds.Contains(cm.LoanApplicationId))
                .ToListAsync(cancellationToken))
                .GroupBy(cm => cm.LoanApplicationId)
                .ToDictionary(g => g.Key, g => g.ToList());

        var seenSubjectIds = new HashSet<Guid>();
        var idRecords = new List<string?[]>();
        var ciRecords = new List<string?[]>();

        foreach (var account in accounts.OrderBy(a => a.LoanAcctNo))
        {
            if (seenSubjectIds.Add(account.CustomerId))
                idRecords.Add(CicRecordBuilder.BuildIdRecord(account.Customer, providerCode, account.Customer.LastModifiedAt ?? account.Customer.CreatedAt));

            var coMakers = account.LoanApplicationId is { } appId && coMakersByApplicationId.TryGetValue(appId, out var cms) ? cms : [];
            foreach (var coMaker in coMakers.Where(cm => cm.CoMakerCustomer is not null))
            {
                if (seenSubjectIds.Add(coMaker.CoMakerCustomerId!.Value))
                    idRecords.Add(CicRecordBuilder.BuildIdRecord(coMaker.CoMakerCustomer!, providerCode, coMaker.CoMakerCustomer!.LastModifiedAt ?? coMaker.CoMakerCustomer!.CreatedAt));
            }

            var unpaidLines = account.ScheduleLines.Where(l => l.Status != "PAID" && l.Status != "SUPERSEDED").ToList();
            var overdueLines = unpaidLines.Where(l => l.DueDate <= asOfDate).ToList();
            var orderedLines = account.ScheduleLines.OrderBy(l => l.InstallmentNo).ToList();
            var firstLine = orderedLines.FirstOrDefault();

            cisaByAccountId.TryGetValue(account.Id, out var cisaRow);

            ciRecords.Add(CicRecordBuilder.BuildCiRecord(
                account,
                providerCode,
                asOfDate,
                cisaRow?.OutstandingPrincipalBalance ?? 0,
                cisaRow?.WasRestructured ?? false,
                cisaRow?.WriteOffDate,
                orderedLines.Count,
                firstLine?.DueDate,
                firstLine?.TotalDue ?? 0,
                unpaidLines.Count,
                overdueLines.Count,
                overdueLines.Sum(l => l.TotalDue - l.PrincipalPaid - l.InterestPaid - l.PenaltyPaid),
                cisaRow?.DaysOverdue ?? 0,
                coMakers));
        }

        return Result.Success(new CicCsdfExport(providerCode, asOfDate, idRecords, ciRecords));
    }
}
