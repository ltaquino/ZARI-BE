using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.Reports.CashReceiptsBook;
using ZARI.Application.Features.Sales.Reports.SalesBook;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>Server-computed Sales BIR-book reports (Sales Book, Cash Receipts Book), each with a
/// JSON route for the FE's on-screen table and a sibling PDF route for filing/printing.</summary>
// Named SalesReportsEndpoints (not ReportsEndpoints) — Api.Endpoints already has a same-named
// ReportsEndpoints class for Purchasing's reports (same namespace, would collide).
public static class SalesReportsEndpoints
{
    public static void MapSalesReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales/reports")
            .WithTags("SalesReports")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/sales-book", GetSalesBook)
            .WithName("GetSalesBookReport")
            .WithSummary("The BIR Sales Book — every non-draft, non-cancelled Sales Invoice, VAT-split");

        group.MapGet("/sales-book/pdf", GetSalesBookPdf)
            .WithName("GetSalesBookReportPdf")
            .WithSummary("The BIR Sales Book, as a PDF");

        group.MapGet("/cash-receipts-book", GetCashReceiptsBook)
            .WithName("GetCashReceiptsBookReport")
            .WithSummary("The BIR Cash Receipts Book — every Customer Payment, running total of POSTED payments");

        group.MapGet("/cash-receipts-book/pdf", GetCashReceiptsBookPdf)
            .WithName("GetCashReceiptsBookReportPdf")
            .WithSummary("The BIR Cash Receipts Book, as a PDF");
    }

    private static async Task<IResult> GetSalesBook(
        string? branchId,
        IQueryHandler<GetSalesBookReportQuery, Result<SalesBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesBookReportQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetSalesBookPdf(
        string? branchId,
        IQueryHandler<GetSalesBookReportQuery, Result<SalesBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesBookReportQuery(branchId), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();

        var bytes = new SalesBookDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "sales-book.pdf");
    }

    private static async Task<IResult> GetCashReceiptsBook(
        string? branchId,
        IQueryHandler<GetCashReceiptsBookReportQuery, Result<CashReceiptsBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCashReceiptsBookReportQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCashReceiptsBookPdf(
        string? branchId,
        IQueryHandler<GetCashReceiptsBookReportQuery, Result<CashReceiptsBookReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCashReceiptsBookReportQuery(branchId), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();

        var bytes = new CashReceiptsBookDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "cash-receipts-book.pdf");
    }
}
