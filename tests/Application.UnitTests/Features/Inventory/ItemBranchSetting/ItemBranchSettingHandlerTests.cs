namespace ZARI.Application.UnitTests.Features.Inventory.ItemBranchSetting;

using ZARI.Application.Features.Inventory.ItemBranchSettings.Create;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Delete;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Get;
using ZARI.Application.Features.Inventory.ItemBranchSettings.GetAll;
using ZARI.Application.Features.Inventory.ItemBranchSettings.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateItemBranchSettingCommandHandlerTests
{
    private static CreateItemBranchSettingCommand Command(Guid itemId, string branchId, Guid? warehouseId = null) =>
        new(itemId, branchId, warehouseId, 10, 5, 100, null, null, "active");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, Guid itemId, Guid warehouseId)> Seed()
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
        return (db, branch, item.Id, warehouse.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Setting()
    {
        var (db, branch, itemId, warehouseId) = await Seed();

        var result = await new CreateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(itemId, branch.Id, warehouseId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DefaultWarehouseId.Should().Be(warehouseId);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, itemId, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Create, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new CreateItemBranchSettingCommandHandler(db, permissions).HandleAsync(Command(itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branch, _, _) = await Seed();

        var result = await new CreateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(Guid.NewGuid(), branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Item_Branch_Pair()
    {
        var (db, branch, itemId, _) = await Seed();
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(itemId, branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new CreateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemBranchSetting.Duplicate");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, itemId, _) = await Seed();

        var result = await new CreateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(itemId, "br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Default_Warehouse_Not_Found()
    {
        var (db, branch, itemId, _) = await Seed();

        var result = await new CreateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(itemId, branch.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }
}

public sealed class UpdateItemBranchSettingCommandHandlerTests
{
    private static UpdateItemBranchSettingCommand Command(Guid id, Guid itemId, string branchId, Guid? warehouseId = null) =>
        new(id, itemId, branchId, warehouseId, 20, 10, 200, null, null, "inactive");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, Guid itemId, ZARI.Domain.Entities.ItemBranchSetting setting)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var setting = InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id);
        db.ItemBranchSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, item.Id, setting);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Setting()
    {
        var (db, branch, itemId, setting) = await Seed();

        var result = await new UpdateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(setting.Id, itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.ItemBranchSettings.FindAsync([setting.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, branch, itemId, _) = await Seed();

        var result = await new UpdateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(Guid.NewGuid(), itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, itemId, setting) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new UpdateItemBranchSettingCommandHandler(db, permissions).HandleAsync(Command(setting.Id, itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Item_Branch_Pair()
    {
        var (db, branch, itemId, setting) = await Seed();
        var other = InventoryTestFixtures.ItemBranchSetting(itemId, "br-2");
        db.Branches.Add(LoanTestFixtures.Branch(id: "br-2"));
        db.ItemBranchSettings.Add(other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new UpdateItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command(other.Id, itemId, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemBranchSetting.Duplicate");
        await db.DisposeAsync();
    }
}

public sealed class DeleteItemBranchSettingCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ItemBranchSetting setting)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var setting = InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id);
        db.ItemBranchSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, setting);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Setting()
    {
        var (db, setting) = await Seed();

        var result = await new DeleteItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new DeleteItemBranchSettingCommand(setting.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.ItemBranchSettings.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new DeleteItemBranchSettingCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new DeleteItemBranchSettingCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, setting) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.Delete, setting.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new DeleteItemBranchSettingCommandHandler(db, permissions).HandleAsync(new DeleteItemBranchSettingCommand(setting.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}

public sealed class GetItemBranchSettingQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Setting_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var setting = InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id);
        db.ItemBranchSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetItemBranchSettingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemBranchSettingQuery(setting.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemId.Should().Be(item.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetItemBranchSettingQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemBranchSettingQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var setting = InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id);
        db.ItemBranchSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("ITEM_BRANCH_SETTINGS", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetItemBranchSettingQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetItemBranchSettingQuery(setting.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllItemBranchSettingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Settings()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllItemBranchSettingsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllItemBranchSettingsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_BRANCH_SETTINGS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllItemBranchSettingsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllItemBranchSettingsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
