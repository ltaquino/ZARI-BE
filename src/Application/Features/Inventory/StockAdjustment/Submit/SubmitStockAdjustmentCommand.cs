namespace ZARI.Application.Features.Inventory.StockAdjustments.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitStockAdjustmentCommand(Guid Id, string RequestedBy) : ICommand<Result<StockAdjustmentResponse>>;
