namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReceiptPo;

using ZARI.Application.Features.Purchasing.GoodsReceiptPos.Submit;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class SubmitGoodsReceiptPoCommandHandlerTests
{
    private static SubmitGoodsReceiptPoCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReceiptPo receipt)> Seed(string status = "DRAFT", bool withLines = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var receipt = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: status);
        if (!withLines) receipt.Lines.Clear();
        db.GoodsReceiptPos.Add(receipt);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, receipt);
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_Draft_Receipt_For_Approval()
    {
        var (db, receipt) = await Seed();

        var result = await Handler(db).HandleAsync(new SubmitGoodsReceiptPoCommand(receipt.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "GOODS_RECEIPT_PO");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new SubmitGoodsReceiptPoCommand(Guid.NewGuid(), "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, receipt) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RECEIPT_PO", FormAction.Edit, receipt.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new SubmitGoodsReceiptPoCommand(receipt.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, receipt) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new SubmitGoodsReceiptPoCommand(receipt.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, receipt) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new SubmitGoodsReceiptPoCommand(receipt.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NoLines");
        await db.DisposeAsync();
    }
}
