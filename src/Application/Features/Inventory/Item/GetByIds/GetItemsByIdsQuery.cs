namespace ZARI.Application.Features.Inventory.Items.GetByIds;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record GetItemsByIdsQuery(List<Guid> Ids) : IQuery<Result<List<ItemResponse>>>;
