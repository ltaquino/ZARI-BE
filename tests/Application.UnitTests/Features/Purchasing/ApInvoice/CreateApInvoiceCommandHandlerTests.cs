namespace ZARI.Application.UnitTests.Features.Purchasing.ApInvoice;

using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateApInvoiceCommandHandlerTests
{
    private static CreateApInvoiceCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateApInvoiceCommand ItemCommand(string branchId, Guid supplierId, Guid grpoId, Guid grpoLineId, Guid itemId, Guid uomId, decimal qty = 5, string invoiceNo = "SINV-0001") =>
        new(branchId, supplierId, "ITEM", grpoId, invoiceNo, DateTimeOffset.UtcNow, null, "remarks", null, "encoder",
            [new ApInvoiceLineInput(itemId, qty, uomId, 50, grpoLineId)], []);

    private static CreateApInvoiceCommand ExpenseCommand(string branchId, Guid supplierId, Guid glAccountId, decimal amount = 500, string invoiceNo = "SINV-0002") =>
        new(branchId, supplierId, "EXPENSE", null, invoiceNo, DateTimeOffset.UtcNow, null, "utility bill", null, "encoder",
            [], [new ApInvoiceExpenseLineInput(glAccountId, "electricity", amount)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid supplierId, Guid itemId, Guid uomId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, supplier.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Item_Invoice_Against_Posted_Grpo()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouse.Id, supplierId, itemId, uomId, status: "POSTED", qty: 10);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, supplierId, grpo.Id, grpo.Lines[0].Id, itemId, uomId, qty: 4), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Expense_Invoice()
    {
        var (db, branchId, supplierId, _, _) = await Seed();
        var glAccount = LoanTestFixtures.GlAccount(code: "6100", name: "Utilities Expense", accountType: "Expense");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ExpenseCommand(branchId, supplierId, glAccount.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpenseLines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(ItemCommand(branchId, supplierId, Guid.NewGuid(), Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(ItemCommand("br-missing", supplierId, Guid.NewGuid(), Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Not_Found()
    {
        var (db, branchId, _, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Supplier.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Supplier_Invoice()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouse.Id, supplierId, itemId, uomId, status: "POSTED", qty: 10);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var existing = PurchasingTestFixtures.ApInvoice(branchId, supplierId, itemId, uomId);
        existing.SupplierInvoiceNo = "SINV-0001";
        db.ApInvoices.Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, supplierId, grpo.Id, grpo.Lines[0].Id, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.DuplicateSupplierInvoice");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Grpo_Not_Found()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, supplierId, Guid.NewGuid(), Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GoodsReceiptPo.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Grpo_Not_Posted()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouse.Id, supplierId, itemId, uomId, status: "DRAFT");
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, supplierId, grpo.Id, grpo.Lines[0].Id, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.GrpoNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Mismatch()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        var otherSupplier = PurchasingTestFixtures.Supplier(code: "SUP2");
        db.Suppliers.Add(otherSupplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouse.Id, supplierId, itemId, uomId, status: "POSTED");
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, otherSupplier.Id, grpo.Id, grpo.Lines[0].Id, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.SupplierMismatch");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Received_Qty()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branchId, warehouse.Id, supplierId, itemId, uomId, status: "POSTED", qty: 5);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(branchId, supplierId, grpo.Id, grpo.Lines[0].Id, itemId, uomId, qty: 10), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.ExceedsReceivedQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Not_Found_For_Expense_Invoice()
    {
        var (db, branchId, supplierId, _, _) = await Seed();

        var result = await Handler(db).HandleAsync(ExpenseCommand(branchId, supplierId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Expense_Account_Is_Not_An_Expense_Type()
    {
        var (db, branchId, supplierId, _, _) = await Seed();
        var glAccount = LoanTestFixtures.GlAccount(code: "1400", name: "Inventory Asset", accountType: "Asset");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ExpenseCommand(branchId, supplierId, glAccount.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.InvalidExpenseAccount");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, supplierId, _, _) = await Seed();
        var glAccount = LoanTestFixtures.GlAccount(code: "6100", name: "Utilities Expense", accountType: "Expense");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new CreateApInvoiceCommand(branchId, supplierId, "EXPENSE", null, "SINV-0003", DateTimeOffset.UtcNow, null, null, Guid.NewGuid(), "encoder",
            [], [new ApInvoiceExpenseLineInput(glAccount.Id, "electricity", 500)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }
}
