namespace ZARI.Application.Features.Inventory.Items.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Domain.Common;

public sealed record GetAllItemsQuery : IQuery<Result<List<ItemResponse>>>;
