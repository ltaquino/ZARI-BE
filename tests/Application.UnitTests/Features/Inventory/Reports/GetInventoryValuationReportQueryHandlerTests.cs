namespace ZARI.Application.UnitTests.Features.Inventory.Reports;

using ZARI.Application.Features.Inventory.Reports.InventoryValuation;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class GetInventoryValuationReportQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", ZARI.Domain.Common.FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetInventoryValuationReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Balances_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetInventoryValuationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Branches.Should().BeEmpty();
        result.Value.GrandTotalValue.Should().Be(0);
        result.Value.GrandTotalQty.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Roll_Up_By_Branch_Then_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var branchA = LoanTestFixtures.Branch("br-a");
        db.Branches.Add(branchA);
        var branchB = LoanTestFixtures.Branch("br-b");
        db.Branches.Add(branchB);
        var warehouseA = InventoryTestFixtures.Warehouse(branchA.Id);
        db.Warehouses.Add(warehouseA);
        var warehouseB = InventoryTestFixtures.Warehouse(branchB.Id, code: "WH2");
        db.Warehouses.Add(warehouseB);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var categorizedItem = InventoryTestFixtures.Item(uom.Id, code: "ITEM-CAT");
        categorizedItem.CategoryId = category.Id;
        db.Items.Add(categorizedItem);
        var uncategorizedItem = InventoryTestFixtures.Item(uom.Id, code: "ITEM-NOCAT");
        db.Items.Add(uncategorizedItem);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockBalances.Add(new StockBalance { ItemId = categorizedItem.Id, BranchId = branchA.Id, WarehouseId = warehouseA.Id, QtyOnHand = 10, AvgUnitCost = 5, TotalValue = 50 });
        db.StockBalances.Add(new StockBalance { ItemId = uncategorizedItem.Id, BranchId = branchA.Id, WarehouseId = warehouseA.Id, QtyOnHand = 4, AvgUnitCost = 25, TotalValue = 100 });
        db.StockBalances.Add(new StockBalance { ItemId = categorizedItem.Id, BranchId = branchB.Id, WarehouseId = warehouseB.Id, QtyOnHand = 2, AvgUnitCost = 5, TotalValue = 10 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryValuationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GrandTotalValue.Should().Be(160);
        result.Value.GrandTotalQty.Should().Be(16);
        result.Value.Branches.Should().HaveCount(2);
        var branchAGroup = result.Value.Branches.Should().ContainSingle(b => b.BranchId == branchA.Id).Subject;
        branchAGroup.BranchTotalValue.Should().Be(150);
        branchAGroup.Categories.Should().HaveCount(2);
        branchAGroup.Categories.Should().ContainSingle(c => c.CategoryId == category.Id && c.CategoryName == category.Name && c.TotalValue == 50);
        branchAGroup.Categories.Should().ContainSingle(c => c.CategoryId == null && c.CategoryName == "Uncategorized" && c.TotalValue == 100);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branchA = LoanTestFixtures.Branch("br-a");
        db.Branches.Add(branchA);
        var branchB = LoanTestFixtures.Branch("br-b");
        db.Branches.Add(branchB);
        var warehouseA = InventoryTestFixtures.Warehouse(branchA.Id);
        db.Warehouses.Add(warehouseA);
        var warehouseB = InventoryTestFixtures.Warehouse(branchB.Id, code: "WH2");
        db.Warehouses.Add(warehouseB);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockBalances.Add(new StockBalance { ItemId = item.Id, BranchId = branchA.Id, WarehouseId = warehouseA.Id, QtyOnHand = 10, AvgUnitCost = 5, TotalValue = 50 });
        db.StockBalances.Add(new StockBalance { ItemId = item.Id, BranchId = branchB.Id, WarehouseId = warehouseB.Id, QtyOnHand = 2, AvgUnitCost = 5, TotalValue = 10 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryValuationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(branchA.Id, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Branches.Should().ContainSingle(b => b.BranchId == branchA.Id);
        result.Value.GrandTotalValue.Should().Be(50);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var categorizedItem = InventoryTestFixtures.Item(uom.Id, code: "ITEM-CAT");
        categorizedItem.CategoryId = category.Id;
        db.Items.Add(categorizedItem);
        var uncategorizedItem = InventoryTestFixtures.Item(uom.Id, code: "ITEM-NOCAT");
        db.Items.Add(uncategorizedItem);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.StockBalances.Add(new StockBalance { ItemId = categorizedItem.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyOnHand = 10, AvgUnitCost = 5, TotalValue = 50 });
        db.StockBalances.Add(new StockBalance { ItemId = uncategorizedItem.Id, BranchId = branch.Id, WarehouseId = warehouse.Id, QtyOnHand = 4, AvgUnitCost = 25, TotalValue = 100 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetInventoryValuationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetInventoryValuationReportQuery(null, category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GrandTotalValue.Should().Be(50);
        result.Value.Branches.Should().ContainSingle().Which.Categories.Should().ContainSingle(c => c.CategoryId == category.Id);
    }
}
