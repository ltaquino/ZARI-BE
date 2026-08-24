namespace ZARI.Application.Features.Inventory.StockAdjustments.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record CancelStockAdjustmentCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<StockAdjustmentResponse>>;
