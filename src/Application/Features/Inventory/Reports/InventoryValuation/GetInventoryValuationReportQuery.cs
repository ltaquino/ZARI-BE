namespace ZARI.Application.Features.Inventory.Reports.InventoryValuation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Current on-hand stock value, rolled up Branch -&gt; Category. BranchId/CategoryId narrow
/// the result when given; either or both omitted returns every branch's/category's.</summary>
public sealed record GetInventoryValuationReportQuery(string? BranchId, Guid? CategoryId) : IQuery<Result<InventoryValuationReportResponse>>;

public sealed record InventoryValuationReportResponse(
    List<InventoryValuationBranchGroup> Branches,
    decimal GrandTotalValue,
    decimal GrandTotalQty);

public sealed record InventoryValuationBranchGroup(
    string BranchId,
    decimal BranchTotalValue,
    List<InventoryValuationCategoryRow> Categories);

public sealed record InventoryValuationCategoryRow(
    Guid? CategoryId,
    string CategoryName,
    decimal QtyOnHand,
    decimal TotalValue);
