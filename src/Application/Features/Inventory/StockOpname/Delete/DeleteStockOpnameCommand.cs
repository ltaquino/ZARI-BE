namespace ZARI.Application.Features.Inventory.StockOpnames.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteStockOpnameCommand(Guid Id) : ICommand<Result>;
