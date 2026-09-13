namespace ZARI.Application.UnitTests.Features.Inventory.Warehouse;

using ZARI.Application.Features.Inventory.Warehouses.Create;
using ZARI.Application.Features.Inventory.Warehouses.Delete;
using ZARI.Application.Features.Inventory.Warehouses.Get;
using ZARI.Application.Features.Inventory.Warehouses.GetAll;
using ZARI.Application.Features.Inventory.Warehouses.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateWarehouseCommandHandlerTests
{
    private static CreateWarehouseCommand Command(string branchId, string code = "WH1") => new(branchId, code, "Main Warehouse", "Main", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Warehouse()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("WH1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("WAREHOUSES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateWarehouseCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.Warehouses.Add(InventoryTestFixtures.Warehouse(branch.Id, code: "WH1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.DuplicateCode");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}

public sealed class UpdateWarehouseCommandHandlerTests
{
    private static UpdateWarehouseCommand Command(Guid id, string branchId, string code = "WH1") => new(id, branchId, code, "Updated", "Main", "inactive");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Warehouse warehouse)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id, code: "WH1");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, warehouse);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Warehouse()
    {
        var (db, branch, warehouse) = await Seed();
        var handler = new UpdateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(warehouse.Id, branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Warehouses.FindAsync([warehouse.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, branch, _) = await Seed();
        var handler = new UpdateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Existing_Warehouse_Branch()
    {
        var (db, branch, warehouse) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("WAREHOUSES", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateWarehouseCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(warehouse.Id, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        var (db, branch, warehouse) = await Seed();
        db.Warehouses.Add(InventoryTestFixtures.Warehouse(branch.Id, code: "WH2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(warehouse.Id, branch.Id, code: "WH2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.DuplicateCode");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_New_Branch_Not_Found()
    {
        var (db, _, warehouse) = await Seed();
        var handler = new UpdateWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(warehouse.Id, "br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }
}

public sealed class DeleteWarehouseCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.Warehouse warehouse)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, warehouse);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Warehouse()
    {
        var (db, _, warehouse) = await Seed();
        var handler = new DeleteWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Warehouses.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, warehouse) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("WAREHOUSES", FormAction.Delete, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteWarehouseCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Storage_Locations()
    {
        var (db, _, warehouse) = await Seed();
        db.StorageLocations.Add(InventoryTestFixtures.StorageLocation(warehouse.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.HasStorageLocations");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_In_Use_By_An_ItemBranchSetting()
    {
        var (db, branch, warehouse) = await Seed();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id, warehouse.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.InUse");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Stock_Reservations()
    {
        var (db, branch, warehouse) = await Seed();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockReservations.Add(new ZARI.Domain.Entities.StockReservation
        {
            ItemId = item.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyReserved = 5,
            ReservedDate = DateTimeOffset.UtcNow, Status = "ACTIVE"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWarehouseCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWarehouseCommand(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.HasReservations");
        await db.DisposeAsync();
    }
}

public sealed class GetWarehouseQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Warehouse_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetWarehouseQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWarehouseQuery(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(warehouse.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetWarehouseQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWarehouseQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("WAREHOUSES", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetWarehouseQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetWarehouseQuery(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllWarehousesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Warehouses()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Warehouses.AddRange(InventoryTestFixtures.Warehouse(branch.Id, code: "WH1"), InventoryTestFixtures.Warehouse(branch.Id, code: "WH2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllWarehousesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllWarehousesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("WAREHOUSES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllWarehousesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllWarehousesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
