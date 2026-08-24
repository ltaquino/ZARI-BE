namespace ZARI.Application.Features.Inventory.StockOpnames.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record RejectStockOpnameCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<StockOpnameResponse>>;
