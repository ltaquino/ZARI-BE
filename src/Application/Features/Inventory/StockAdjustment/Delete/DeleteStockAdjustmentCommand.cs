namespace ZARI.Application.Features.Inventory.StockAdjustments.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteStockAdjustmentCommand(Guid Id) : ICommand<Result>;
