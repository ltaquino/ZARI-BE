namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record GetPurchaseRequestQuery(Guid Id) : IQuery<Result<PurchaseRequestResponse>>;
