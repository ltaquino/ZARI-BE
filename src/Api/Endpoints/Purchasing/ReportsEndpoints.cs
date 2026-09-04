using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Reports.ApAging;
using ZARI.Application.Features.Purchasing.Reports.CashOutRegister;
using ZARI.Application.Features.Purchasing.Reports.GrniReconciliation;
using ZARI.Application.Features.Purchasing.Reports.PurchaseBook;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ReportsEndpoints
{
    public static void MapPurchasingReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchasing/reports").WithTags("PurchasingReports").WithGroupName("Purchasing").RequireAuthorization();

        group.MapGet("/ap-aging", GetApAging)
            .WithName("GetApAgingReport")
            .WithSummary("AP Aging report — posted, unpaid AP invoices grouped by supplier and how overdue they are");

        group.MapGet("/ap-aging/pdf", GetApAgingPdf)
            .WithName("GetApAgingReportPdf")
            .WithSummary("AP Aging report as a PDF");

        group.MapGet("/grni-reconciliation", GetGrni)
            .WithName("GetGrniReconciliationReport")
            .WithSummary("GRNI Reconciliation report — outstanding Goods Received Not Invoiced, cross-checked against the live GL balance");

        group.MapGet("/grni-reconciliation/pdf", GetGrniPdf)
            .WithName("GetGrniReconciliationReportPdf")
            .WithSummary("GRNI Reconciliation report as a PDF");

        group.MapGet("/purchase-book", GetPurchaseBook)
            .WithName("GetPurchaseBookReport")
            .WithSummary("Purchase Book (BIR books of accounts) report");

        group.MapGet("/purchase-book/pdf", GetPurchaseBookPdf)
            .WithName("GetPurchaseBookReportPdf")
            .WithSummary("Purchase Book report as a PDF");

        group.MapGet("/cash-out-register", GetCashOutRegister)
            .WithName("GetCashOutRegisterReport")
            .WithSummary("Cash-Out Register (Cash Disbursements Book) report");

        group.MapGet("/cash-out-register/pdf", GetCashOutRegisterPdf)
            .WithName("GetCashOutRegisterReportPdf")
            .WithSummary("Cash-Out Register report as a PDF");
    }

    private static async Task<IResult> GetApAging(
        string? branchId,
        Guid? supplierId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetApAgingReportQuery, Result<ApAgingReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetApAgingReportQuery(branchId, supplierId, asOfDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetApAgingPdf(
        string? branchId,
        Guid? supplierId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetApAgingReportQuery, Result<ApAgingReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetApAgingReportQuery(branchId, supplierId, asOfDate), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new ApAgingDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "ap-aging-report.pdf");
    }

    private static async Task<IResult> GetGrni(
        string? branchId,
        bool? showOnlyOutstanding,
        IQueryHandler<GetGrniReconciliationReportQuery, Result<GrniReconciliationReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(branchId, showOnlyOutstanding ?? false), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetGrniPdf(
        string? branchId,
        bool? showOnlyOutstanding,
        IQueryHandler<GetGrniReconciliationReportQuery, Result<GrniReconciliationReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(branchId, showOnlyOutstanding ?? false), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new GrniReconciliationDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "grni-reconciliation-report.pdf");
    }

    private static async Task<IResult> GetPurchaseBook(
        string? branchId,
        Guid? supplierId,
        IQueryHandler<GetPurchaseBookReportQuery, Result<PurchaseBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(branchId, supplierId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetPurchaseBookPdf(
        string? branchId,
        Guid? supplierId,
        IQueryHandler<GetPurchaseBookReportQuery, Result<PurchaseBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPurchaseBookReportQuery(branchId, supplierId), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new PurchaseBookDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "purchase-book-report.pdf");
    }

    private static async Task<IResult> GetCashOutRegister(
        string? branchId,
        Guid? bankAccountId,
        IQueryHandler<GetCashOutRegisterReportQuery, Result<CashOutRegisterReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCashOutRegisterReportQuery(branchId, bankAccountId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCashOutRegisterPdf(
        string? branchId,
        Guid? bankAccountId,
        IQueryHandler<GetCashOutRegisterReportQuery, Result<CashOutRegisterReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCashOutRegisterReportQuery(branchId, bankAccountId), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new CashOutRegisterDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "cash-out-register-report.pdf");
    }
}
