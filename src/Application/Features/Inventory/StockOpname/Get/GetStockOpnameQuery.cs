namespace ZARI.Application.Features.Inventory.StockOpnames.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Domain.Common;

public sealed record GetStockOpnameQuery(Guid Id) : IQuery<Result<StockOpnameResponse>>;
