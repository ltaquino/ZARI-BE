namespace ZARI.Application.UnitTests.Features.Inventory.StorageLocation;

using ZARI.Application.Features.Inventory.StorageLocations.Create;
using ZARI.Application.Features.Inventory.StorageLocations.Delete;
using ZARI.Application.Features.Inventory.StorageLocations.Get;
using ZARI.Application.Features.Inventory.StorageLocations.GetAll;
using ZARI.Application.Features.Inventory.StorageLocations.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateStorageLocationCommandHandlerTests
{
    private static CreateStorageLocationCommand Command(Guid warehouseId) => new(warehouseId, "A", "1", "1", "01");

    [Fact]
    public async Task HandleAsync_Should_Create_Location()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(warehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WarehouseId.Should().Be(warehouse.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateStorageLocationCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }
}

public sealed class UpdateStorageLocationCommandHandlerTests
{
    private static UpdateStorageLocationCommand Command(Guid id, Guid warehouseId) => new(id, warehouseId, "B", "2", "2", "02");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid warehouseId, ZARI.Domain.Entities.StorageLocation location)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var location = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, warehouse.Id, location);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Location()
    {
        var (db, warehouseId, location) = await Seed();
        var handler = new UpdateStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(location.Id, warehouseId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.StorageLocations.FindAsync([location.Id], TestContext.Current.CancellationToken))!.Zone.Should().Be("B");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, warehouseId, _) = await Seed();
        var handler = new UpdateStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), warehouseId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, warehouseId, location) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateStorageLocationCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(location.Id, warehouseId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        var (db, _, location) = await Seed();
        var handler = new UpdateStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(location.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
        await db.DisposeAsync();
    }
}

public sealed class DeleteStorageLocationCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StorageLocation location)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var location = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, location);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Location_With_No_Referential_Guard()
    {
        var (db, location) = await Seed();
        var handler = new DeleteStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStorageLocationCommand(location.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.StorageLocations.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteStorageLocationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStorageLocationCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, location) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteStorageLocationCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteStorageLocationCommand(location.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}

public sealed class GetStorageLocationQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Location_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var location = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(location);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetStorageLocationQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStorageLocationQuery(location.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WarehouseId.Should().Be(warehouse.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetStorageLocationQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStorageLocationQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetStorageLocationQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetStorageLocationQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllStorageLocationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Locations()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StorageLocations.AddRange(InventoryTestFixtures.StorageLocation(warehouse.Id, binCode: "01"), InventoryTestFixtures.StorageLocation(warehouse.Id, binCode: "02"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStorageLocationsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStorageLocationsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STORAGE_LOCATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStorageLocationsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStorageLocationsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
