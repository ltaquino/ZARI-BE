namespace ZARI.Application.Features.Inventory.StockOpnames.Post;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record PostStockOpnameCommand(Guid Id, string PostedBy) : ICommand<Result<StockOpnameResponse>>;
