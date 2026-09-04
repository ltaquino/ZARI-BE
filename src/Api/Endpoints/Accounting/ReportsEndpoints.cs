using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.Reports.GeneralJournal;
using ZARI.Application.Features.Accounting.Reports.GlAccountLedger;
using ZARI.Application.Features.Accounting.Reports.TrialBalance;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Server-side computed accounting reports (Trial Balance, GL Account Ledger, General Journal) plus
/// a QuestPDF export for each — the report pages that previously computed everything client-side in
/// React from raw unpaged list data.
/// </summary>
public static class AccountingReportsEndpoints
{
    public static void MapAccountingReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounting/reports").WithTags("AccountingReports").WithGroupName("Accounting").RequireAuthorization();

        group.MapGet("/trial-balance", GetTrialBalance).WithName("GetTrialBalanceReport");
        group.MapGet("/trial-balance/pdf", GetTrialBalancePdf).WithName("GetTrialBalanceReportPdf");
        group.MapGet("/gl-account-ledger", GetGlAccountLedger).WithName("GetGlAccountLedgerReport");
        group.MapGet("/gl-account-ledger/pdf", GetGlAccountLedgerPdf).WithName("GetGlAccountLedgerReportPdf");
        group.MapGet("/general-journal", GetGeneralJournal).WithName("GetGeneralJournalReport");
        group.MapGet("/general-journal/pdf", GetGeneralJournalPdf).WithName("GetGeneralJournalReportPdf");
    }

    private static async Task<IResult> GetTrialBalance(
        string? branchId, DateTimeOffset? asOfDate, bool? includeZeroBalances,
        IQueryHandler<GetTrialBalanceReportQuery, Result<TrialBalanceReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(branchId, asOfDate ?? DateTimeOffset.UtcNow, includeZeroBalances ?? false), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetTrialBalancePdf(
        string? branchId, DateTimeOffset? asOfDate, bool? includeZeroBalances,
        IQueryHandler<GetTrialBalanceReportQuery, Result<TrialBalanceReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrialBalanceReportQuery(branchId, asOfDate ?? DateTimeOffset.UtcNow, includeZeroBalances ?? false), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();
        var bytes = new TrialBalanceDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "trial-balance.pdf");
    }

    private static async Task<IResult> GetGlAccountLedger(
        Guid accountId, string? branchId, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        IQueryHandler<GetGlAccountLedgerReportQuery, Result<GlAccountLedgerReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGlAccountLedgerReportQuery(accountId, branchId, fromDate, toDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetGlAccountLedgerPdf(
        Guid accountId, string? branchId, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        IQueryHandler<GetGlAccountLedgerReportQuery, Result<GlAccountLedgerReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGlAccountLedgerReportQuery(accountId, branchId, fromDate, toDate), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();
        var bytes = new GlAccountLedgerDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "gl-account-ledger.pdf");
    }

    private static async Task<IResult> GetGeneralJournal(
        string? branchId, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        IQueryHandler<GetGeneralJournalReportQuery, Result<GeneralJournalReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGeneralJournalReportQuery(branchId, fromDate, toDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetGeneralJournalPdf(
        string? branchId, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        IQueryHandler<GetGeneralJournalReportQuery, Result<GeneralJournalReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGeneralJournalReportQuery(branchId, fromDate, toDate), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();
        var bytes = new GeneralJournalDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "general-journal.pdf");
    }
}
