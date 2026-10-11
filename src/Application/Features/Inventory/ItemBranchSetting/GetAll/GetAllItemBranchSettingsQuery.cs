namespace ZARI.Application.Features.Inventory.ItemBranchSettings.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Domain.Common;

/// <param name="BranchId">Only this branch's settings (a device only needs its own branches').</param>
// Delta sync (ZARI Mobile): only rows created or changed at/after this instant. Deletions aren't reported — a client
// still does an occasional full refresh to prune.
public sealed record GetAllItemBranchSettingsQuery(string? BranchId = null, DateTimeOffset? UpdatedSince = null) : IQuery<Result<List<ItemBranchSettingResponse>>>;
