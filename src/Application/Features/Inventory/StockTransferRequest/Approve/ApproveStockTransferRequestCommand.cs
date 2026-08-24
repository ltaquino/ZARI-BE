namespace ZARI.Application.Features.Inventory.StockTransferRequests.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveStockTransferRequestCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<StockTransferRequestResponse>>;
