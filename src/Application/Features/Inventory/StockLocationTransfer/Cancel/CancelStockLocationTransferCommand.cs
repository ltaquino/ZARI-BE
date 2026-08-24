namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockLocationTransfers.GetAll;
using ZARI.Domain.Common;

public sealed record CancelStockLocationTransferCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<StockLocationTransferResponse>>;
