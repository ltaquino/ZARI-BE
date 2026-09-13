namespace ZARI.Application.UnitTests.Features.Inventory.Item;

using ZARI.Application.Features.Inventory.Items.Create;
using ZARI.Application.Features.Inventory.Items.Delete;
using ZARI.Application.Features.Inventory.Items.Get;
using ZARI.Application.Features.Inventory.Items.GetAll;
using ZARI.Application.Features.Inventory.Items.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateItemCommandHandlerTests
{
    private static CreateItemCommand Command(Guid baseUomId, string code = "ITEM-1", Guid? categoryId = null, bool isTileDisplay = false, bool isSold = true) =>
        new(code, "Test Item", null, categoryId, baseUomId, "FinishedGood", "Avg", false, false, isSold, true, true, isTileDisplay, false, null, null, null, null, "VATABLE", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Item()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("ITEM-1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateItemCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: "ITEM-1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Category_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id, categoryId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Tile_Display_Without_Sold()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id, isTileDisplay: true, isSold: false), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.TileDisplayRequiresSold");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_With_A_Real_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id, categoryId: category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CategoryId.Should().Be(category.Id);
    }
}

public sealed class UpdateItemCommandHandlerTests
{
    private static UpdateItemCommand Command(Guid id, Guid baseUomId, string code = "ITEM-1", bool isTileDisplay = false, bool isSold = true) =>
        new(id, code, "Updated", null, null, baseUomId, "FinishedGood", "Avg", false, false, isSold, true, true, isTileDisplay, false, null, null, null, null, "VATABLE", "inactive");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid uomId, ZARI.Domain.Entities.Item item)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, uom.Id, item);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Item()
    {
        var (db, uomId, item) = await Seed();
        var handler = new UpdateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(item.Id, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Items.FindAsync([item.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, uomId, _) = await Seed();
        var handler = new UpdateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, uomId, item) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateItemCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(item.Id, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        var (db, uomId, item) = await Seed();
        db.Items.Add(InventoryTestFixtures.Item(uomId, code: "ITEM-2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(item.Id, uomId, code: "ITEM-2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, _, item) = await Seed();
        var handler = new UpdateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(item.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Tile_Display_Without_Sold()
    {
        var (db, uomId, item) = await Seed();
        var handler = new UpdateItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(item.Id, uomId, isTileDisplay: true, isSold: false), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.TileDisplayRequiresSold");
        await db.DisposeAsync();
    }
}

public sealed class DeleteItemCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Item item)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, item);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Item()
    {
        var (db, item) = await Seed();
        var handler = new DeleteItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCommand(item.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Items.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, item) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteItemCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteItemCommand(item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Reorder_Settings()
    {
        var (db, item) = await Seed();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCommand(item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.HasReorderSettings");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Stock_Reservations()
    {
        var (db, item) = await Seed();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockReservations.Add(new ZARI.Domain.Entities.StockReservation
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 5,
            ReservedDate = DateTimeOffset.UtcNow, Status = "ACTIVE"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteItemCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCommand(item.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.HasReservations");
        await db.DisposeAsync();
    }
}

public sealed class GetItemQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Item_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetItemQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemQuery(item.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(item.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetItemQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetItemQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetItemQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllItemsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Items.AddRange(InventoryTestFixtures.Item(uom.Id, code: "ITEM-1"), InventoryTestFixtures.Item(uom.Id, code: "ITEM-2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllItemsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllItemsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllItemsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
