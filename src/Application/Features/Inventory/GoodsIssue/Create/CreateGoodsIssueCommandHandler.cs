namespace ZARI.Application.Features.Inventory.GoodsIssues.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.GoodsIssues.GetAll;
using ZARI.Application.Features.Inventory.GoodsIssues.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Abstractions.Identity;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateGoodsIssueCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateGoodsIssueCommand, Result<GoodsIssueResponse>>
{
    public async Task<Result<GoodsIssueResponse>> HandleAsync(CreateGoodsIssueCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<GoodsIssueResponse>(Error.Forbidden("GoodsIssue.Forbidden", "You do not have permission to create goods issues for this branch."));

        var warehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.WarehouseId, cancellationToken);
        if (!warehouseExists)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Warehouse.NotFound", $"Warehouse with ID '{command.WarehouseId}' was not found."));

        if (command.DestWarehouseId.HasValue)
        {
            var destWarehouseExists = await dbContext.Warehouses.AnyAsync(w => w.Id == command.DestWarehouseId.Value, cancellationToken);
            if (!destWarehouseExists)
                return Result.Failure<GoodsIssueResponse>(Error.NotFound("Warehouse.NotFound", $"Destination warehouse with ID '{command.DestWarehouseId}' was not found."));
        }

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        if (command.ReferenceType == "STOCK_TRANSFER" && command.DestBranchId is not null)
        {
            var destBranchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.DestBranchId, cancellationToken);
            if (!destBranchExists)
                return Result.Failure<GoodsIssueResponse>(Error.NotFound("Branch.NotFound", $"Destination branch with ID '{command.DestBranchId}' was not found."));
        }

        var itemIds = command.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await dbContext.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        if (items.Count != itemIds.Count)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Item.NotFound", "One or more items on this issue were not found."));

        var uomIds = command.Lines.Select(l => l.UomId).Distinct().ToList();
        var uoms = await dbContext.Uoms.Where(u => uomIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        if (uoms.Count != uomIds.Count)
            return Result.Failure<GoodsIssueResponse>(Error.NotFound("Uom.NotFound", "One or more units of measure on this issue were not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "GI"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(numberResult.Error!);

        var isTransfer = command.ReferenceType == "STOCK_TRANSFER";
        var issue = new GoodsIssue
        {
            GiNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            WarehouseId = command.WarehouseId,
            ReferenceType = command.ReferenceType,
            DestBranchId = isTransfer ? command.DestBranchId : null,
            DestWarehouseId = isTransfer ? command.DestWarehouseId : null,
            ReasonCode = isTransfer ? null : command.ReasonCode,
            GiDate = command.GiDate,
            Status = "DRAFT",
            Remarks = command.Remarks,
            StockTransferRequestRefNo = command.StockTransferRequestRefNo,
            StockTransferRequestId = command.StockTransferRequestId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new GoodsIssueLine
            {
                ItemId = l.ItemId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                QtyIssued = l.QtyIssued,
                UomId = l.UomId,
                UnitCost = l.UnitCost
            }).ToList()
        };

        dbContext.GoodsIssues.Add(issue);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in issue.Lines)
        {
            line.Item = items[line.ItemId];
            line.Uom = uoms[line.UomId];
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("GOODS_ISSUE", issue.Id.ToString(), issue.BranchId, "CREATED", "ACTIVITY",
                "created this goods issue", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<GoodsIssueResponse>(notifyResult.Error!);

        return Result.Success(GoodsIssueMapper.ToResponse(issue));
    }
}
