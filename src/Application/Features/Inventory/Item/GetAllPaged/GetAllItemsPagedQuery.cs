namespace ZARI.Application.Features.Inventory.Items.GetAllPaged;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record GetAllItemsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<ItemResponse>>>;
