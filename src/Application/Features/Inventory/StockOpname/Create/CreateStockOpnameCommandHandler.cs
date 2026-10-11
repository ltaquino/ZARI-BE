namespace ZARI.Application.Features.Inventory.StockOpnames.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.StockOpnames.GetAll;
using ZARI.Application.Features.Inventory.StockOpnames.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStockOpnameCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateStockOpnameCommand, Result<StockOpnameResponse>>
{
    public async Task<Result<StockOpnameResponse>> HandleAsync(CreateStockOpnameCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("STOCK_OPNAMES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<StockOpnameResponse>(Error.Forbidden("StockOpname.Forbidden", "You do not have permission to create stock counts for this branch."));

        // A resend from an offline client: hand back the stock count the first send created.
        if (command.ClientReferenceId is { } clientReferenceId
            && await dbContext.StockOpnames.AsNoTracking().Include(o => o.Lines).ThenInclude(l => l.Item).ThenInclude(i => i.BaseUom)
                .FirstOrDefaultAsync(x => x.ClientReferenceId == clientReferenceId, cancellationToken) is { } existing)
            return existing.BranchId == command.BranchId
                ? Result.Success(StockOpnameMapper.ToResponse(existing))
                : Result.Failure<StockOpnameResponse>(Error.Conflict("StockOpname.ClientReferenceReused", "That client reference was already used for another branch's stock count."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Include(i => i.BaseUom).Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<StockOpnameResponse>(Error.NotFound("Item.NotFound", "One or more items on this stock count were not found."));

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<StockOpnameResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "OPN"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(numberResult.Error!);

        var opname = new StockOpname
        {
            OpnameNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            CountDate = command.CountDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            ClientReferenceId = command.ClientReferenceId,
            Lines = command.Lines.Select(l => new StockOpnameLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                SystemQty = l.SystemQty,
                CountedQty = l.CountedQty,
                VarianceQty = l.CountedQty - l.SystemQty,
                UnitCost = l.UnitCost
            }).ToList()
        };

        dbContext.StockOpnames.Add(opname);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in opname.Lines)
        {
            line.Item = items[line.ItemId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("STOCK_OPNAME", opname.Id.ToString(), opname.BranchId, "CREATED", "ACTIVITY",
                "created this stock count", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<StockOpnameResponse>(notifyResult.Error!);

        return Result.Success(StockOpnameMapper.ToResponse(opname));
    }
}
