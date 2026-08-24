namespace ZARI.Application.Features.Inventory.StockAdjustments.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectStockAdjustmentCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<StockAdjustmentResponse>>;
