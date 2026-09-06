namespace ZARI.Application.Features.Inventory.Items.Search;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record SearchItemsQuery(string? Q, int Limit = 8) : IQuery<Result<List<ItemResponse>>>;
