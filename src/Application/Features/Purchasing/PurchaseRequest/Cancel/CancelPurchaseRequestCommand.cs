namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record CancelPurchaseRequestCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<PurchaseRequestResponse>>;
