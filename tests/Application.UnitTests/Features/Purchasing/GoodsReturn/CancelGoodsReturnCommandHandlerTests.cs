namespace ZARI.Application.UnitTests.Features.Purchasing.GoodsReturn;

using ZARI.Application.Features.Purchasing.GoodsReturns.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CancelGoodsReturnCommandHandlerTests
{
    private static CancelGoodsReturnCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GoodsReturn goodsReturn)> Seed(string status = "DRAFT")
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
        var goodsReturn = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: status);
        db.GoodsReturns.Add(goodsReturn);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, goodsReturn);
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_Draft_Return()
    {
        var (db, goodsReturn) = await Seed();

        var result = await Handler(db).HandleAsync(new CancelGoodsReturnCommand(goodsReturn.Id, "encoder", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new CancelGoodsReturnCommand(Guid.NewGuid(), "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, goodsReturn) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_RETURNS", FormAction.Cancel, goodsReturn.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CancelGoodsReturnCommand(goodsReturn.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, goodsReturn) = await Seed(status: "CANCELLED");

        var result = await Handler(db).HandleAsync(new CancelGoodsReturnCommand(goodsReturn.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Posted_Requires_Cancellation_Request()
    {
        var (db, goodsReturn) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new CancelGoodsReturnCommand(goodsReturn.Id, "encoder", "x"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReturn.RequiresCancellationRequest");
        await db.DisposeAsync();
    }
}
