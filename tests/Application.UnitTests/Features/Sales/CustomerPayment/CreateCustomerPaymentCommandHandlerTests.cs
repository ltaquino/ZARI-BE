namespace ZARI.Application.UnitTests.Features.Sales.CustomerPayment;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateCustomerPaymentCommandHandlerTests
{
    private static CreateCustomerPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateCustomerPaymentCommand Command(string branchId, Guid customerId, Guid cashAccountId, Guid salesInvoiceId, decimal amount = 100) =>
        new(branchId, customerId, DateTimeOffset.UtcNow, "cash receipt", null, "encoder",
            [new CustomerPaymentLineInput(salesInvoiceId, amount)], PaymentMethod: "CASH", CashAccountId: cashAccountId);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid customerId, Guid cashAccountId, Guid salesInvoiceId)> Seed(string invoiceStatus = "POSTED", decimal invoiceQty = 1, decimal invoiceUnitPrice = 100)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var cashGlAccount = LoanTestFixtures.GlAccount(code: "1000");
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        db.GlAccounts.AddRange(cashGlAccount, arGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = SalesTestFixtures.SalesInvoice(branch.Id, customer.Id, item.Id, uom.Id, status: invoiceStatus, qty: invoiceQty, unitPrice: invoiceUnitPrice);
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, customer.Id, cashGlAccount.Id, invoice.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Payment()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_Create_Via_Pos_Mode_Permission_Alone()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, customerId, cashAccountId, salesInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        var (db, branchId, _, cashAccountId, salesInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Not_Found()
    {
        var (db, branchId, customerId, _, salesInvoiceId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, Guid.NewGuid(), salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Invoice()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed(invoiceQty: 2);
        var command = new CreateCustomerPaymentCommand(branchId, customerId, DateTimeOffset.UtcNow, null, null, "encoder",
            [new CustomerPaymentLineInput(salesInvoiceId, 50), new CustomerPaymentLineInput(salesInvoiceId, 50)], PaymentMethod: "CASH", CashAccountId: cashAccountId);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.DuplicateInvoice");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Sales_Invoice_Not_Found()
    {
        var (db, branchId, customerId, cashAccountId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, cashAccountId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Mismatch()
    {
        var (db, branchId, _, cashAccountId, salesInvoiceId) = await Seed();
        var otherCustomer = LoanTestFixtures.Customer(branchId, name: "Other Customer");
        db.Customers.Add(otherCustomer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, otherCustomer.Id, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.CustomerMismatch");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_Not_Payable()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed(invoiceStatus: "DRAFT");

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.InvoiceNotPayable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Exceeds_Balance()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed(invoiceQty: 1, invoiceUnitPrice: 100);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId, amount: 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.AmountExceedsBalance");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var command = Command(branchId, customerId, cashAccountId, salesInvoiceId) with { CostCenterId = Guid.NewGuid() };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Resolve_Funding_From_Tenders_When_Supplied()
    {
        var (db, branchId, customerId, cashGlAccountId, salesInvoiceId) = await Seed();
        var paymentMethod = LoanTestFixtures.PaymentMethod(cashGlAccountId, code: "GCASH", name: "GCash");
        db.PaymentMethods.Add(paymentMethod);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new CreateCustomerPaymentCommand(branchId, customerId, DateTimeOffset.UtcNow, null, null, "encoder",
            [new CustomerPaymentLineInput(salesInvoiceId, 100)], Tenders: [new CustomerPaymentTenderInput(paymentMethod.Id, 100, null, null)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentMethod.Should().Be("GCash");
        result.Value!.CashAccountId.Should().Be(cashGlAccountId);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Tender_Total_Mismatches_Applied_Total()
    {
        var (db, branchId, customerId, cashGlAccountId, salesInvoiceId) = await Seed();
        var paymentMethod = LoanTestFixtures.PaymentMethod(cashGlAccountId, code: "GCASH");
        db.PaymentMethods.Add(paymentMethod);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new CreateCustomerPaymentCommand(branchId, customerId, DateTimeOffset.UtcNow, null, null, "encoder",
            [new CustomerPaymentLineInput(salesInvoiceId, 100)], Tenders: [new CustomerPaymentTenderInput(paymentMethod.Id, 60, null, null)]);

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CustomerPayment.TenderMismatch");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Quick_Post_When_Company_Toggle_Enabled()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var company = SystemModuleTestFixtures.Company();
        company.CustomerPaymentQuickPostEnabled = true;
        db.Companies.Add(company);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, cashAccountId, salesInvoiceId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "CustomerPayment");
        (await db.SalesInvoices.FindAsync([salesInvoiceId], TestContext.Current.CancellationToken))!.Status.Should().Be("PAID");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Force_Quick_Post_When_Flag_Set_Regardless_Of_Company_Toggle()
    {
        var (db, branchId, customerId, cashAccountId, salesInvoiceId) = await Seed();
        var command = Command(branchId, customerId, cashAccountId, salesInvoiceId) with { ForceQuickPost = true };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        await db.DisposeAsync();
    }
}
