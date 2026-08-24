namespace ZARI.Application.Features.Inventory.StockAdjustments.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record RequestStockAdjustmentCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<StockAdjustmentResponse>>;
