namespace ZARI.Application.UnitTests.Features.Purchasing.ApInvoice;

using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.Purchasing.ApInvoices.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateApInvoiceCommandHandlerTests
{
    private static UpdateApInvoiceCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateApInvoiceCommand ItemCommand(Guid id, Guid itemId, Guid uomId, Guid grpoLineId, decimal qty = 3, string invoiceNo = "SINV-0001") =>
        new(id, invoiceNo, DateTimeOffset.UtcNow, null, "revised", null, "encoder", [new ApInvoiceLineInput(itemId, qty, uomId, 50, grpoLineId)], []);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.ApInvoice invoice, Guid itemId, Guid uomId, Guid grpoLineId)> Seed(string status = "DRAFT")
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
        var grpo = PurchasingTestFixtures.GoodsReceiptPo(branch.Id, warehouse.Id, supplier.Id, item.Id, uom.Id, status: "POSTED", qty: 10);
        db.GoodsReceiptPos.Add(grpo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: status, goodsReceiptPoId: grpo.Id, goodsReceiptPoLineId: grpo.Lines[0].Id);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, invoice, item.Id, uom.Id, grpo.Lines[0].Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Item_Invoice()
    {
        var (db, invoice, itemId, uomId, grpoLineId) = await Seed();

        var result = await Handler(db).HandleAsync(ItemCommand(invoice.Id, itemId, uomId, grpoLineId, qty: 3), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.Qty == 3);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(ItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, invoice, itemId, uomId, grpoLineId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Edit, invoice.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(ItemCommand(invoice.Id, itemId, uomId, grpoLineId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, invoice, itemId, uomId, grpoLineId) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(ItemCommand(invoice.Id, itemId, uomId, grpoLineId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Expense_Lines_Sent_For_Item_Invoice()
    {
        var (db, invoice, itemId, uomId, grpoLineId) = await Seed();
        var command = new UpdateApInvoiceCommand(invoice.Id, "SINV-0001", DateTimeOffset.UtcNow, null, null, null, "encoder",
            [new ApInvoiceLineInput(itemId, 3, uomId, 50, grpoLineId)], [new ApInvoiceExpenseLineInput(Guid.NewGuid(), "x", 100)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.InvalidLinesForType");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, invoice, _, _, _) = await Seed();
        var command = new UpdateApInvoiceCommand(invoice.Id, "SINV-0001", DateTimeOffset.UtcNow, null, null, null, "encoder", [], []);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NoLines");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Supplier_Invoice()
    {
        var (db, invoice, itemId, uomId, grpoLineId) = await Seed();
        var other = PurchasingTestFixtures.ApInvoice(invoice.BranchId, invoice.SupplierId, itemId, uomId);
        other.SupplierInvoiceNo = "SINV-DUP";
        db.ApInvoices.Add(other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(ItemCommand(invoice.Id, itemId, uomId, grpoLineId, invoiceNo: "SINV-DUP"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.DuplicateSupplierInvoice");
        await db.DisposeAsync();
    }
}
