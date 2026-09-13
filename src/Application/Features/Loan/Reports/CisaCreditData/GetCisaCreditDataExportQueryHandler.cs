namespace ZARI.Application.Features.Loan.Reports.CisaCreditData;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.Reports.CisaCreditData.Shared;
using ZARI.Domain.Common;

/// <summary>
/// Builds the RA 9510 (Credit Information System Act) / CDA MC 2019-01 "Basic Credit Data" extract
/// — data capture and export only, not a live CIC submission (see Customer.cs's doc comment for
/// why: MC 2019-01 doesn't publish CIC's actual file format). One row per LoanAccount; everything
/// that's derivable from existing Loan data is computed live here rather than read off a stored
/// snapshot column, same rule this whole module follows elsewhere (LoanAmortizationScheduleLine's
/// paid amounts, the Delinquency report's aging, etc.).
/// </summary>
public sealed class GetCisaCreditDataExportQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetCisaCreditDataExportQuery, Result<List<CisaCreditDataRow>>>
{
    public async Task<Result<List<CisaCreditDataRow>>> HandleAsync(GetCisaCreditDataExportQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, cancellationToken))
            return Result.Failure<List<CisaCreditDataRow>>(Error.Forbidden("CisaCreditData.Forbidden", "You do not have permission to view loan accounts."));

        var accountsQuery = dbContext.LoanAccounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.ScheduleLines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.BranchId)) accountsQuery = accountsQuery.Where(a => a.BranchId == query.BranchId);
        if (query.CustomerId is { } customerId) accountsQuery = accountsQuery.Where(a => a.CustomerId == customerId);

        var accounts = await accountsQuery.ToListAsync(cancellationToken);
        if (accounts.Count == 0) return Result.Success(new List<CisaCreditDataRow>());

        var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;
        var accountIds = accounts.Select(a => a.Id).ToList();
        var customerIds = accounts.Select(a => a.CustomerId).Distinct().ToList();

        // Ledger's last entry per account gives both the current running balance and the last
        // activity date in one pass — same "trust the ledger, not a cached column" rule as every
        // other Loan balance lookup in this module.
        var lastLedgerByAccount = await dbContext.LoanLedgerEntries.AsNoTracking()
            .Where(l => accountIds.Contains(l.LoanAccountId))
            .GroupBy(l => l.LoanAccountId)
            .Select(g => g.OrderByDescending(l => l.SequenceNo).First())
            .ToDictionaryAsync(l => l.LoanAccountId, cancellationToken);

        var postedRestructurings = await dbContext.LoanRestructurings.AsNoTracking()
            .Where(r => r.Status == "POSTED" && (accountIds.Contains(r.OldLoanAccountId) || (r.NewLoanAccountId != null && accountIds.Contains(r.NewLoanAccountId.Value))))
            .Select(r => new { r.OldLoanAccountId, r.NewLoanAccountId })
            .ToListAsync(cancellationToken);
        var restructuredAccountIds = postedRestructurings
            .SelectMany(r => r.NewLoanAccountId is { } newId ? new[] { r.OldLoanAccountId, newId } : new[] { r.OldLoanAccountId })
            .Distinct()
            .ToList();

        // Dictionary<Guid, DateTimeOffset> (non-nullable value) would make a missing key's
        // TryGetValue out-param default to 0001-01-01 rather than null once assigned into a
        // DateTimeOffset? row field — the value type has to be nullable here for a "no write-off"
        // account to actually come out null instead of that bogus default date.
        var writeOffDateByAccount = await dbContext.LoanWriteOffs.AsNoTracking()
            .Where(w => w.Status == "POSTED" && accountIds.Contains(w.LoanAccountId))
            .ToDictionaryAsync(w => w.LoanAccountId, w => (DateTimeOffset?)w.WriteOffDate, cancellationToken);

        var negativeRecordsByCustomer = await dbContext.CustomerCreditRecords.AsNoTracking()
            .Where(r => customerIds.Contains(r.CustomerId))
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync(cancellationToken);
        var negativeSummaryByCustomer = negativeRecordsByCustomer
            .GroupBy(r => r.CustomerId)
            .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(r => $"{r.RecordType} ({r.RecordDate:yyyy-MM-dd}): {r.Description}")));

        var rows = accounts.Select(account =>
        {
            var unpaidLines = account.ScheduleLines.Where(l => l.Status != "PAID" && l.Status != "SUPERSEDED").ToList();
            var overdueLines = unpaidLines.Where(l => l.DueDate <= asOfDate).ToList();
            var oldestOverdue = overdueLines.OrderBy(l => l.DueDate).FirstOrDefault();
            var daysOverdue = oldestOverdue is null ? 0 : (int)Math.Floor((asOfDate - oldestOverdue.DueDate).TotalDays);

            lastLedgerByAccount.TryGetValue(account.Id, out var lastLedger);
            writeOffDateByAccount.TryGetValue(account.Id, out var writeOffDate);
            negativeSummaryByCustomer.TryGetValue(account.CustomerId, out var negativeSummary);

            return new CisaCreditDataRow(
                account.CustomerId,
                account.Customer.MemberNo,
                account.Customer.Name,
                account.Customer.Tin,
                account.Customer.SssOrGsisNo,
                account.Customer.DateOfBirth,
                account.Customer.Sex,
                account.Customer.CivilStatus,
                account.Customer.DependentsCount,
                account.Customer.Employer,
                account.Customer.EmployerPosition,
                account.Customer.NetIncomeLastYear,
                account.Customer.ResidenceSince,
                account.Customer.PriorResidenceHistory,
                account.Customer.EmploymentSince,
                account.Customer.PriorEmploymentHistory,
                account.Customer.HousingStatus,
                account.Customer.OwnsVehicle,
                account.Customer.BankAccountInfo,
                account.Customer.OtherAssetsNotes,
                account.Customer.DataSharingConsent,
                account.Customer.DataSharingConsentDate,
                account.Id,
                account.LoanAcctNo,
                account.BranchId,
                account.LoanProduct.Name,
                account.LoanProduct.RequiresCollateral,
                account.GrantDate,
                account.PrincipalAmount,
                lastLedger?.RunningPrincipalBalance ?? 0,
                account.RepaymentFrequency,
                unpaidLines.Count,
                account.Status,
                lastLedger?.EntryDate,
                oldestOverdue?.DueDate,
                daysOverdue,
                CisaAgingBucket.Of(daysOverdue),
                restructuredAccountIds.Contains(account.Id),
                writeOffDate,
                account.IsDisputed,
                account.DisputeNotes,
                negativeSummary);
        })
        .OrderBy(r => r.CustomerName).ThenBy(r => r.LoanAcctNo)
        .ToList();

        return Result.Success(rows);
    }
}
