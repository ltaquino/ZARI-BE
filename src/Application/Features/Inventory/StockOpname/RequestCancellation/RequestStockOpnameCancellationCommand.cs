namespace ZARI.Application.Features.Inventory.StockOpnames.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record RequestStockOpnameCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<StockOpnameResponse>>;
