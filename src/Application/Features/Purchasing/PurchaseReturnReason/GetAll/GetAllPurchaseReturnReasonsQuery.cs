namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Domain.Common;

public sealed record GetAllPurchaseReturnReasonsQuery : IQuery<Result<List<PurchaseReturnReasonResponse>>>;
