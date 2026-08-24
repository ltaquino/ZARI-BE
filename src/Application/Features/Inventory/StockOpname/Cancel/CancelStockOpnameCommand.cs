namespace ZARI.Application.Features.Inventory.StockOpnames.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record CancelStockOpnameCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<StockOpnameResponse>>;
