namespace ZARI.Application.UnitTests.Features.Purchasing.OutgoingPayment;

using ZARI.Application.Features.Purchasing.OutgoingPayments.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateOutgoingPaymentCommandHandlerTests
{
    private static CreateOutgoingPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateOutgoingPaymentCommand Command(string branchId, Guid supplierId, Guid bankAccountId, Guid apInvoiceId, decimal amount = 100) =>
        new(branchId, supplierId, bankAccountId, DateTimeOffset.UtcNow, "CHK-0001", "payment run", null, "encoder",
            [new OutgoingPaymentLineInput(apInvoiceId, amount)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid supplierId, Guid bankAccountId, Guid apInvoiceId)> Seed(string invoiceStatus = "POSTED", decimal invoiceQty = 1, decimal invoiceUnitCost = 100)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var bankGlAccount = LoanTestFixtures.GlAccount(code: "1010");
        db.GlAccounts.Add(bankGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankGlAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id, status: invoiceStatus, qty: invoiceQty, unitCost: invoiceUnitCost);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, supplier.Id, bankAccount.Id, invoice.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Payment()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, supplierId, bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, supplierId, bankAccountId, apInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", supplierId, bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Not_Found()
    {
        var (db, branchId, _, bankAccountId, apInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Supplier.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Bank_Account_Not_Found()
    {
        var (db, branchId, supplierId, _, apInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, Guid.NewGuid(), apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("BankAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Invoice()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed(invoiceQty: 2);
        var command = new CreateOutgoingPaymentCommand(branchId, supplierId, bankAccountId, DateTimeOffset.UtcNow, null, null, null, "encoder",
            [new OutgoingPaymentLineInput(apInvoiceId, 50), new OutgoingPaymentLineInput(apInvoiceId, 50)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.DuplicateInvoice");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Ap_Invoice_Not_Found()
    {
        var (db, branchId, supplierId, bankAccountId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, bankAccountId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApInvoice.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Mismatch()
    {
        var (db, branchId, _, bankAccountId, apInvoiceId) = await Seed();
        var otherSupplier = PurchasingTestFixtures.Supplier(code: "SUP2");
        db.Suppliers.Add(otherSupplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, otherSupplier.Id, bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.SupplierMismatch");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_Not_Payable()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed(invoiceStatus: "DRAFT");

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, bankAccountId, apInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.InvoiceNotPayable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Exceeds_Balance()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed(invoiceQty: 1, invoiceUnitCost: 100);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, bankAccountId, apInvoiceId, amount: 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.AmountExceedsBalance");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed();
        var command = new CreateOutgoingPaymentCommand(branchId, supplierId, bankAccountId, DateTimeOffset.UtcNow, null, null, Guid.NewGuid(), "encoder",
            [new OutgoingPaymentLineInput(apInvoiceId, 100)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_Partial_Payment_Against_Partially_Paid_Invoice()
    {
        var (db, branchId, supplierId, bankAccountId, apInvoiceId) = await Seed(invoiceStatus: "PARTIALLY_PAID", invoiceQty: 1, invoiceUnitCost: 100);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, bankAccountId, apInvoiceId, amount: 30), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }
}
