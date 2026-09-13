namespace ZARI.Application.UnitTests.Features.Inventory.StockTransferRequest;

using ZARI.Application.Features.Inventory.StockTransferRequests.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllStockTransferRequestsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STOCK_TRANSFER_REQUESTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStockTransferRequestsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStockTransferRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Requests_Ordered_By_Date_Descending()
    {
        await using var db = TestDbContextFactory.Create();
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
        var older = InventoryTestFixtures.StockTransferRequest(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id);
        older.RequestDate = DateTimeOffset.UtcNow.AddDays(-5);
        var newer = InventoryTestFixtures.StockTransferRequest(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id);
        newer.RequestDate = DateTimeOffset.UtcNow;
        db.StockTransferRequests.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStockTransferRequestsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStockTransferRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(newer.Id);
    }
}
