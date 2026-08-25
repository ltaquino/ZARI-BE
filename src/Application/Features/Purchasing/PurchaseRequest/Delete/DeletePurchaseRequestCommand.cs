namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeletePurchaseRequestCommand(Guid Id) : ICommand<Result>;
