namespace ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllPurchaseRequestsQuery : IQuery<Result<List<PurchaseRequestResponse>>>;

public sealed record PurchaseRequestLineResponse(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? ItemDescription,
    decimal QtyRequested,
    Guid UomId,
    string UomCode,
    DateTimeOffset? NeededByDate);

public sealed record PurchaseRequestResponse(
    Guid Id,
    string RequestNo,
    string BranchId,
    DateTimeOffset RequestDate,
    string Status,
    string? Remarks,
    List<PurchaseRequestLineResponse> Lines,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
