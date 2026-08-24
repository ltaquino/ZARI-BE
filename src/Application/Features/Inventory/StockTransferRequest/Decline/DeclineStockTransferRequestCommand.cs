namespace ZARI.Application.Features.Inventory.StockTransferRequests.Decline;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record DeclineStockTransferRequestCommand(Guid Id, string DeclinedBy, string Reason) : ICommand<Result<StockTransferRequestResponse>>;
