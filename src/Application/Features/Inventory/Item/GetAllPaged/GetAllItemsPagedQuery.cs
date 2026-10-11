namespace ZARI.Application.Features.Inventory.Items.GetAllPaged;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

// Delta sync (ZARI Mobile): only rows created or changed at/after this instant. Deletions aren't reported — a client
// still does an occasional full refresh to prune.
public sealed record GetAllItemsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null, DateTimeOffset? UpdatedSince = null) : IQuery<Result<PagedResult<ItemResponse>>>;
