namespace ZARI.Application.Features.Inventory.Items.TileMenu;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record GetTileMenuItemsQuery(string BranchId) : IQuery<Result<List<TileMenuItemResponse>>>;

// Carries the full ItemResponse (not a slim projection) — a tapped tile is routed straight into
// PosPage's existing handleScan(item, qty), which needs the full item (IsSerialized, BaseUomId,
// VatType, etc.), not just what the tile itself renders.
public sealed record TileMenuItemResponse(ItemResponse Item, decimal Price);
