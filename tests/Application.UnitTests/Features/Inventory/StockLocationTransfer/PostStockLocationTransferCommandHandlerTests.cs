namespace ZARI.Application.UnitTests.Features.Inventory.StockLocationTransfer;

using ZARI.Application.Features.Inventory.StockLocationBalances.Move;
using ZARI.Application.Features.Inventory.StockLocationTransfers.Post;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// No approval workflow (DRAFT -> POSTED directly) — but the real success path still isn't
/// InMemory-testable: MoveBetweenLocationsCommand opens a real transaction (faked here), and the
/// handler's own final status flip uses ExecuteUpdateAsync (also unsupported). Only the guard
/// clauses before that point are exercised.
/// </summary>
public sealed class PostStockLocationTransferCommandHandlerTests
{
    private static PostStockLocationTransferCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<MoveBetweenLocationsCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockLocationTransfer transfer, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT", bool withLines = true)
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
        var fromLocation = InventoryTestFixtures.StorageLocation(warehouse.Id);
        db.StorageLocations.Add(fromLocation);
        var toLocation = InventoryTestFixtures.StorageLocation(warehouse.Id, binCode: "02");
        db.StorageLocations.Add(toLocation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = InventoryTestFixtures.StockLocationTransfer(branch.Id, warehouse.Id, item.Id, fromLocation.Id, toLocation.Id, status: status);
        if (!withLines)
            transfer.Lines.Clear();
        db.StockLocationTransfers.Add(transfer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, transfer, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new PostStockLocationTransferCommand(Guid.NewGuid(), "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, transfer, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_LOCATION_TRANSFERS", FormAction.Approve, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new PostStockLocationTransferCommand(transfer.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, transfer, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new PostStockLocationTransferCommand(transfer.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockLocationTransfer.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, transfer, _) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new PostStockLocationTransferCommand(transfer.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockLocationTransfer.NoLines");
        await db.DisposeAsync();
    }
}
