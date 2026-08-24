namespace ZARI.Application.Features.Inventory.StockTransferRequests.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record RejectStockTransferRequestCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<StockTransferRequestResponse>>;
