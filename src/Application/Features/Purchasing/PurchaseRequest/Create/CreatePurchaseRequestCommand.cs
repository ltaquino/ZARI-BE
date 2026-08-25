namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record PurchaseRequestLineInput(Guid ItemId, decimal QtyRequested, Guid UomId, DateTimeOffset? NeededByDate);

public sealed record CreatePurchaseRequestCommand(
    string BranchId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? CreatedBy,
    List<PurchaseRequestLineInput> Lines) : ICommand<Result<PurchaseRequestResponse>>;
