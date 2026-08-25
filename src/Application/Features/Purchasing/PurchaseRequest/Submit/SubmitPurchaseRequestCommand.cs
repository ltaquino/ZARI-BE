namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitPurchaseRequestCommand(Guid Id, string RequestedBy) : ICommand<Result<PurchaseRequestResponse>>;
