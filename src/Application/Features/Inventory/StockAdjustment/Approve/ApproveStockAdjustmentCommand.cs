namespace ZARI.Application.Features.Inventory.StockAdjustments.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockAdjustments.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveStockAdjustmentCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<StockAdjustmentResponse>>;
