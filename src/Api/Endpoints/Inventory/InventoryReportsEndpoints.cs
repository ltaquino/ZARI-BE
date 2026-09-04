using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Reports.InventoryValuation;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>Server-computed Inventory Valuation report — Branch -&gt; Category rollup of today's live
/// StockBalance snapshot — with a JSON route for the FE's on-screen table and a sibling PDF route.</summary>
public static class InventoryReportsEndpoints
{
    public static void MapInventoryReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/reports")
            .WithTags("InventoryReports")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/inventory-valuation", GetInventoryValuation)
            .WithName("GetInventoryValuationReport")
            .WithSummary("Inventory Valuation — current on-hand stock value rolled up Branch -> Category");

        group.MapGet("/inventory-valuation/pdf", GetInventoryValuationPdf)
            .WithName("GetInventoryValuationReportPdf")
            .WithSummary("Inventory Valuation report, as a PDF");
    }

    private static async Task<IResult> GetInventoryValuation(
        string? branchId,
        Guid? categoryId,
        IQueryHandler<GetInventoryValuationReportQuery, Result<InventoryValuationReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(branchId, categoryId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetInventoryValuationPdf(
        string? branchId,
        Guid? categoryId,
        IQueryHandler<GetInventoryValuationReportQuery, Result<InventoryValuationReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(branchId, categoryId), cancellationToken);
        if (!result.IsSuccess) return result.ToProblemDetails();

        var bytes = new InventoryValuationDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "inventory-valuation.pdf");
    }
}
