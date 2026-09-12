namespace ZARI.Application.UnitTests.Features.Purchasing.Reports.GrniReconciliation;

using ZARI.Application.Features.Purchasing.Reports.GrniReconciliation;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetGrniReconciliationReportQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Branch branch, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, warehouse.Id, supplier.Id, item.Id, uom.Id);
    }

    private static GlJournal GrniJournal(string branchId, Guid grniAccountId, decimal creditAmount) => new()
    {
        JournalNo = $"JV-{Guid.NewGuid():N}", BranchId = branchId, JournalDate = DateTimeOffset.UtcNow, SourceModule = "TEST",
        SourceReferenceTable = "Test", SourceReferenceId = "doc-1", Status = "POSTED",
        Lines = [new GlJournalLine { AccountId = grniAccountId, DebitAmount = 0, CreditAmount = creditAmount }]
    };

    [Fact]
    public async Task HandleAsync_Should_Report_Full_Value_As_Outstanding_When_Nothing_Cleared()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Rows.Single();
        row.Value.Should().Be(500);
        row.ClearedValue.Should().Be(0);
        row.Outstanding.Should().Be(500);
        result.Value!.TotalOutstanding.Should().Be(500);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Reduce_Outstanding_When_Ap_Invoice_Clears_The_Line()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50);
        db.GoodsReceiptPos.Add(grpo);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 55, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        var row = result.Value!.Rows.Single();
        // Cleared value is qty-weighted at the GRPO line's own unit cost (50), not the invoice's (55).
        row.ClearedValue.Should().Be(500);
        row.Outstanding.Should().Be(0);
        row.ClearedByDocumentNos.Should().Contain(invoice.InvoiceNo);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Reduce_Outstanding_When_Goods_Return_Clears_The_Line()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50);
        db.GoodsReceiptPos.Add(grpo);
        var ret = PurchasingTestFixtures.GoodsReturn(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 4, unitCost: 50, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.GoodsReturns.Add(ret);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        var row = result.Value!.Rows.Single();
        row.ClearedValue.Should().Be(200);
        row.Outstanding.Should().Be(300);
        row.ClearedByDocumentNos.Should().Contain(ret.ReturnNo);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Cancelled_Grpos()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "CANCELLED", qty: 10, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Apply_ShowOnlyOutstanding_To_Rows_But_Not_To_Totals()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var clearedGrpo = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 5, unitCost: 20);
        db.GoodsReceiptPos.Add(clearedGrpo);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplierId, itemId, uomId, status: "POSTED", qty: 5, unitCost: 20, goodsReceiptPoId: clearedGrpo.Id, goodsReceiptPoLineId: clearedGrpo.Lines[0].Id);
        db.ApInvoices.Add(invoice);
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null, ShowOnlyOutstanding: true), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        result.Value!.Rows.Single().Outstanding.Should().Be(500);
        result.Value!.TotalReceived.Should().Be(600);
        result.Value!.TotalOutstanding.Should().Be(500);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Be_Reconciled_When_Live_Gl_Balance_Matches_Document_Outstanding()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grniAccount = LoanTestFixtures.GlAccount(code: "2100", name: "GRNI", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.Add(grniAccount);
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.GlJournals.Add(GrniJournal(branch.Id, grniAccount.Id, 500));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.LiveGrniBalance.Should().Be(500);
        result.Value!.Variance.Should().Be(0);
        result.Value!.IsReconciled.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Detect_Variance_When_Gl_Disagrees_With_Documents()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var grniAccount = LoanTestFixtures.GlAccount(code: "2100", name: "GRNI", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.Add(grniAccount);
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.GlJournals.Add(GrniJournal(branch.Id, grniAccount.Id, 300));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        result.Value!.Variance.Should().Be(200);
        result.Value!.IsReconciled.Should().BeFalse();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch()
    {
        var (db, branch, warehouseId, supplierId, itemId, uomId) = await Seed();
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.Add(otherBranch);
        var otherWarehouse = InventoryTestFixtures.Warehouse(otherBranch.Id);
        db.Warehouses.Add(otherWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouseId, supplierId, itemId, uomId, status: "POSTED", qty: 10, unitCost: 50));
        db.GoodsReceiptPos.Add(PurchasingTestFixtures.GoodsReceiptPo(otherBranch.Id, otherWarehouse.Id, supplierId, itemId, uomId, status: "POSTED", qty: 4, unitCost: 50));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGrniReconciliationReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value!.Rows.Should().ContainSingle();
        result.Value!.TotalReceived.Should().Be(500);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GOODS_RECEIPT_PO", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetGrniReconciliationReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGrniReconciliationReportQuery(null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
