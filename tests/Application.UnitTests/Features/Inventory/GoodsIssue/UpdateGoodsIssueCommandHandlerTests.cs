namespace ZARI.Application.UnitTests.Features.Inventory.GoodsIssue;

using ZARI.Application.Features.Inventory.GoodsIssues.Create;
using ZARI.Application.Features.Inventory.GoodsIssues.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateGoodsIssueCommandHandlerTests
{
    private static UpdateGoodsIssueCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateGoodsIssueCommand Command(Guid id, string branchId, Guid warehouseId, Guid itemId, Guid uomId, string referenceType = "INTERNAL_USE", string? reasonCode = "DAMAGE", string? destBranchId = null, Guid? destWarehouseId = null, Guid? costCenterId = null) =>
        new(id, branchId, warehouseId, referenceType, destBranchId, destWarehouseId, reasonCode, DateTimeOffset.UtcNow, "remarks", null, null, costCenterId, "updater",
            [new GoodsIssueLineInput(itemId, "BATCH-1", null, 10, uomId, 50)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.GoodsIssue issue, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Warehouse warehouse, ZARI.Domain.Entities.Item item, ZARI.Domain.Entities.Uom uom)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var issue = InventoryTestFixtures.GoodsIssue(branch.Id, warehouse.Id, item.Id, uom.Id, status: status);
        db.GoodsIssues.Add(issue);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, issue, branch, warehouse, item, uom);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), "br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, issue, branch, warehouse, item, uom) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("GOODS_ISSUES", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, issue, branch, warehouse, item, uom) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsIssue.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, issue, branch, _, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, Guid.NewGuid(), item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, issue, _, warehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, "br-missing", warehouse.Id, item.Id, uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, issue, branch, warehouse, _, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, Guid.NewGuid(), uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, issue, branch, warehouse, item, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, issue, branch, warehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, item.Id, uom.Id, costCenterId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Issue()
    {
        var (db, issue, branch, warehouse, item, uom) = await Seed();

        var result = await Handler(db).HandleAsync(Command(issue.Id, branch.Id, warehouse.Id, item.Id, uom.Id, referenceType: "DISPOSAL", reasonCode: "EXPIRED"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReferenceType.Should().Be("DISPOSAL");
        result.Value.ReasonCode.Should().Be("EXPIRED");
        result.Value.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }
}
