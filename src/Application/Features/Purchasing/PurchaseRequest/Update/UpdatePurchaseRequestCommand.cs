namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Create;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record UpdatePurchaseRequestCommand(
    Guid Id,
    string BranchId,
    DateTimeOffset RequestDate,
    string? Remarks,
    string? UpdatedBy,
    List<PurchaseRequestLineInput> Lines) : ICommand<Result<PurchaseRequestResponse>>;
