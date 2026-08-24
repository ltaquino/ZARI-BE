namespace ZARI.Application.Features.Inventory.StockTransferRequests.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record CancelStockTransferRequestCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<StockTransferRequestResponse>>;
