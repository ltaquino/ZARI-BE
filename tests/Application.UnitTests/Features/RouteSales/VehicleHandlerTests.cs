namespace ZARI.Application.UnitTests.Features.RouteSales;

using ZARI.Application.Features.RouteSales.Vehicles.Create;
using ZARI.Application.Features.RouteSales.Vehicles.Delete;
using ZARI.Application.Features.RouteSales.Vehicles.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateVehicleCommandHandlerTests
{
    private static CreateVehicleCommand Command(string branchId, string code = "TRK1", string plate = "ABC 1234", string prefix = "T1-") =>
        new(branchId, code, plate, "Isuzu Elf", "active", prefix, OrNextNumber: 501, MachineIdentificationNumber: "MIN-1");

    [Fact]
    public async Task HandleAsync_Should_Create_Vehicle_With_Its_Own_Warehouse_And_Own_Series_Terminal()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new CreateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WarehouseCode.Should().Be("TRK-TRK1");
        result.Value.PosTerminalCode.Should().Be("TRK1");
        result.Value.OrNextNumber.Should().Be(501);

        var warehouse = db.Warehouses.Single();
        warehouse.WarehouseType.Should().Be("Vehicle");
        warehouse.BranchId.Should().Be("br-1");

        var terminal = db.PosTerminals.Single();
        terminal.HasOwnSeries.Should().BeTrue();
        terminal.OrPrefix.Should().Be("T1-");
        terminal.MachineIdentificationNumber.Should().Be("MIN-1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("VEHICLES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await new CreateVehicleCommandHandler(db, permissions).HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_A_Duplicate_Plate_No()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        await handler.HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(Command("br-1", code: "TRK2", prefix: "T2-"), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Vehicle.DuplicatePlateNo");
        db.Warehouses.Should().ContainSingle("a rejected vehicle must not leave a warehouse behind");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_A_Prefix_That_Is_A_Branch_Invoice_Series_Prefix()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        db.DocumentSequences.Add(SystemModuleTestFixtures.DocumentSequence("br-1", docType: "BIR-OR", prefix: "HQ-OR-"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new CreateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Command("br-1", prefix: "HQ-OR-"), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("PosTerminal.PrefixClashesWithBranch");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_A_Prefix_Already_Used_By_Another_Truck()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        await handler.HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(Command("br-1", code: "TRK2", plate: "XYZ 9876"), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("PosTerminal.DuplicatePrefix");
    }

    [Fact]
    public void Validator_Should_Require_A_Series_That_Fits_The_Invoice_Number_Column()
    {
        var validator = new CreateVehicleValidator();

        validator.Validate(Command("br-1", prefix: "")).IsValid.Should().BeFalse();
        validator.Validate(Command("br-1") with { OrPrefix = new string('X', 20), OrPaddingLength = 6 }).IsValid.Should().BeFalse();
        validator.Validate(Command("br-1")).IsValid.Should().BeTrue();
    }
}

public sealed class UpdateAndDeleteVehicleCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid vehicleId)> SeedVehicle()
    {
        var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var created = await new CreateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new CreateVehicleCommand("br-1", "TRK1", "ABC 1234", null, "active", "T1-"), TestContext.Current.CancellationToken);
        return (db, created.Value!.Id);
    }

    private static UpdateVehicleCommand Update(Guid id, string status = "active", int? next = null) =>
        new(id, "ABC 1234", "Isuzu Elf", status, "T1-", OrNextNumber: next);

    [Fact]
    public async Task Update_Should_Cascade_Status_To_Warehouse_And_Terminal()
    {
        var (db, id) = await SeedVehicle();

        var result = await new UpdateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Update(id, status: "inactive"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Warehouses.Single().Status.Should().Be("inactive");
        db.PosTerminals.Single().Status.Should().Be("inactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Update_Should_Lock_The_Series_Once_An_Invoice_Number_Was_Issued()
    {
        var (db, id) = await SeedVehicle();
        var terminalId = db.PosTerminals.Single().Id;
        var invoice = SalesTestFixtures.SalesInvoice("br-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        invoice.PosTerminalId = terminalId;
        invoice.BirOrSeriesNumber = "T1-000001";
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new UpdateVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(Update(id, next: 900), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Vehicle.SeriesLocked");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Delete_Should_Remove_Vehicle_Warehouse_And_Terminal_When_Unused()
    {
        var (db, id) = await SeedVehicle();

        var result = await new DeleteVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new DeleteVehicleCommand(id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Vehicles.Should().BeEmpty();
        db.Warehouses.Should().BeEmpty();
        db.PosTerminals.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Delete_Should_Be_Blocked_Once_The_Truck_Has_A_Z_Reading()
    {
        var (db, id) = await SeedVehicle();
        var terminalId = db.PosTerminals.Single().Id;
        db.ZReadings.Add(new ZReading
        {
            BranchId = "br-1", PosTerminalId = terminalId, CounterScope = terminalId.ToString(), ZCounterValue = 1,
            PeriodStart = DateTimeOffset.UtcNow, PeriodEnd = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new DeleteVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new DeleteVehicleCommand(id), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Vehicle.HasZReadings");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Delete_Should_Be_Blocked_Once_The_Truck_Has_Stock_History()
    {
        var (db, id) = await SeedVehicle();
        db.StockLedgers.Add(new StockLedger
        {
            ItemId = Guid.NewGuid(), BranchId = "br-1", WarehouseId = db.Warehouses.Single().Id,
            TransactionType = "IN", ReferenceTable = "Test", ReferenceId = "1", QtyIn = 5,
            TransactionDate = DateTimeOffset.UtcNow, PostedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new DeleteVehicleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new DeleteVehicleCommand(id), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("Vehicle.HasStockHistory");
        await db.DisposeAsync();
    }
}
