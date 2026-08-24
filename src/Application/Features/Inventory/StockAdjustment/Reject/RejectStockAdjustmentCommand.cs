namespace ZARI.Application.Features.Inventory.StockAdjustments.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectStockAdjustmentCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<StockAdjustmentResponse>>;
