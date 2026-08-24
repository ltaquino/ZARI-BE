namespace ZARI.Application.Features.Inventory.StockOpnames.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveStockOpnameCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<StockOpnameResponse>>;
