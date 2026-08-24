namespace ZARI.Application.Features.Inventory.StockTransferRequests.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitStockTransferRequestCommand(Guid Id, string RequestedBy) : ICommand<Result<StockTransferRequestResponse>>;
