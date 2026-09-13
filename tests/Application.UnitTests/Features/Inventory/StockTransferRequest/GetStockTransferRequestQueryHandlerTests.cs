namespace ZARI.Application.UnitTests.Features.Inventory.StockTransferRequest;

using ZARI.Application.Features.Inventory.StockTransferRequests.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetStockTransferRequestQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockTransferRequest request, ZARI.Domain.Entities.Branch sourceBranch, ZARI.Domain.Entities.Branch destBranch)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = InventoryTestFixtures.StockTransferRequest(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id);
        db.StockTransferRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request, sourceBranch, destBranch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetStockTransferRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStockTransferRequestQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Both_Source_And_Dest()
    {
        var (db, request, sourceBranch, destBranch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, sourceBranch.Id, Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, destBranch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetStockTransferRequestQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetStockTransferRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Only_Source_Branch_Permission_Granted()
    {
        // Proves the dual-branch OR-check: the fulfilling (source) branch can view a request that
        // was raised by (and formally belongs to) the requesting (dest) branch.
        var (db, request, sourceBranch, destBranch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, sourceBranch.Id, Arg.Any<CancellationToken>()).Returns(true);
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, destBranch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetStockTransferRequestQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetStockTransferRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Request()
    {
        var (db, request, _, _) = await Seed();
        var handler = new GetStockTransferRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStockTransferRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(request.Id);
        result.Value.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }
}
