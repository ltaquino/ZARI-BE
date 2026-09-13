namespace ZARI.Application.UnitTests.Features.Sales.PosSale;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.PosSale;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The invoice-creation and payment-creation steps are real (CreateSalesInvoiceCommandHandler /
/// CreateCustomerPaymentCommandHandler, each with a real PostGlJournalCommandHandler +
/// GetNextDocumentNumberCommandHandler underneath — same as CustomerPayment/SalesInvoice's own
/// fully-testable Create/Approve). Only IssueStockLinesCommand/IssueSerialCommand are faked, since
/// a real IssueStockLinesCommandHandler opens a transaction the InMemory provider rejects (the same
/// finding as DeliveryOrder/SalesReturn) — PosStockPostingService calls these directly with
/// whatever this handler was constructed with, not through its own DI resolution, so faking just
/// those two still lets the whole checkout (invoice + real AR/Revenue/VAT journal + payment) run
/// for real. The COGS journal itself is skipped (fake stock response returns no costed lines), a
/// documented gap, not a guard-clause limitation.
/// </summary>
public sealed class CreatePosSaleCommandHandlerTests
{
    private static CreatePosSaleCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Messaging.ICommandHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>>? createSalesInvoiceHandler = null)
    {
        var perms = permissions ?? LoanTestFixtures.AllowAllPermissionService();
        var nextDocNumber = new GetNextDocumentNumberCommandHandler(db);
        var postGl = new PostGlJournalCommandHandler(db, nextDocNumber);
        var createNotification = new CreateNotificationCommandHandler(db);
        return new CreatePosSaleCommandHandler(
            db, perms,
            createSalesInvoiceHandler ?? new CreateSalesInvoiceCommandHandler(db, nextDocNumber, postGl, createNotification, perms),
            LoanTestFixtures.SuccessHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>(Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()))),
            LoanTestFixtures.SuccessHandler<IssueSerialCommand, Result>(Result.Success()),
            postGl,
            new CreateCustomerPaymentCommandHandler(db, nextDocNumber, postGl, createNotification, perms));
    }

    private static CreatePosSaleCommand Command(string branchId, Guid terminalId, Guid itemId, Guid uomId, Guid paymentMethodId, Guid? customerId = null, decimal tenderAmount = 100) =>
        new(branchId, terminalId, customerId, DateTimeOffset.UtcNow, null, null,
            [new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null)],
            [new CustomerPaymentTenderInput(paymentMethodId, tenderAmount, null, null)], "cashier");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid terminalId, Guid customerId, Guid itemId, Guid uomId, Guid warehouseId, Guid paymentMethodId)> Seed(bool serialized = false)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        var revenueGlAccount = LoanTestFixtures.GlAccount(code: "4000", name: "Sales Revenue", accountType: "Revenue", normalBalance: "Credit");
        var vatGlAccount = LoanTestFixtures.GlAccount(code: "2200", name: "VAT Payable", accountType: "Liability", normalBalance: "Credit");
        var cashGlAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.AddRange(arGlAccount, revenueGlAccount, vatGlAccount, cashGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        item.IsSerialized = serialized;
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(new ItemBranchSetting { ItemId = item.Id, BranchId = branch.Id, DefaultWarehouseId = warehouse.Id, Status = "active" });
        var paymentMethod = LoanTestFixtures.PaymentMethod(cashGlAccount.Id, code: "CASH", name: "Cash");
        db.PaymentMethods.Add(paymentMethod);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, terminal.Id, customer.Id, item.Id, uom.Id, warehouse.Id, paymentMethod.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Complete_Checkout_With_Exact_Cash_Tender()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId, tenderAmount: 100), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChangeDue.Should().Be(0);
        result.Value!.InvoiceTotal.Should().Be(100);
        (await db.SalesInvoices.FindAsync([result.Value!.SalesInvoiceId], TestContext.Current.CancellationToken))!.Status.Should().Be("PAID");
        (await db.CustomerPayments.FindAsync([result.Value!.CustomerPaymentId], TestContext.Current.CancellationToken))!.Status.Should().Be("POSTED");
        db.GlJournals.Should().Contain(j => j.SourceReferenceTable == "SalesInvoice").And.Contain(j => j.SourceReferenceTable == "CustomerPayment");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Compute_Change_Due_For_Overtendered_Cash()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId, tenderAmount: 150), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChangeDue.Should().Be(50);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cash_Tender_Covers_The_Change_Owed()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var giftCard = LoanTestFixtures.PaymentMethod((await db.GlAccounts.FirstAsync(a => a.Code == "1000", TestContext.Current.CancellationToken)).Id, code: "GC", name: "Gift Card");
        db.PaymentMethods.Add(giftCard);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new CreatePosSaleCommand(branchId, terminalId, customerId, DateTimeOffset.UtcNow, null, null,
            [new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null)],
            [new CustomerPaymentTenderInput(giftCard.Id, 150, null, null)], "cashier");

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.InvalidChange");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Terminal_Not_Found()
    {
        var (db, branchId, _, customerId, itemId, uomId, _, paymentMethodId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.TerminalNotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Terminal_Inactive()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var terminal = await db.PosTerminals.FindAsync([terminalId], TestContext.Current.CancellationToken);
        terminal!.Status = "inactive";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.TerminalInactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branchId, terminalId, customerId, _, uomId, _, paymentMethodId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, Guid.NewGuid(), uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Available_At_Branch()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var setting = await db.ItemBranchSettings.FirstAsync(s => s.ItemId == itemId, TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Remove(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.ItemNotAvailableAtBranch");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Missing_Default_Warehouse()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var setting = await db.ItemBranchSettings.FirstAsync(s => s.ItemId == itemId, TestContext.Current.CancellationToken);
        setting.DefaultWarehouseId = null;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.ItemMissingDefaultWarehouse");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Serial_No_Required_But_Missing()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed(serialized: true);

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.SerialNoRequired");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Serial_No()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed(serialized: true);
        var command = new CreatePosSaleCommand(branchId, terminalId, customerId, DateTimeOffset.UtcNow, null, null,
            [
                new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null, "SN-001"),
                new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null, "SN-001")
            ],
            [new CustomerPaymentTenderInput(paymentMethodId, 200, null, null)], "cashier");

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.DuplicateSerialNo");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Serial_Not_Available()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed(serialized: true);
        var command = new CreatePosSaleCommand(branchId, terminalId, customerId, DateTimeOffset.UtcNow, null, null,
            [new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null, "SN-MISSING")],
            [new CustomerPaymentTenderInput(paymentMethodId, 100, null, null)], "cashier");

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.SerialNotAvailable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Serial_Is_In_Stock()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, warehouseId, paymentMethodId) = await Seed(serialized: true);
        db.SerialNumbers.Add(new SerialNumber { ItemId = itemId, SerialNo = "SN-001", WarehouseId = warehouseId, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new CreatePosSaleCommand(branchId, terminalId, customerId, DateTimeOffset.UtcNow, null, null,
            [new SalesInvoiceLineInput(itemId, 1, uomId, 100, 0, null, null, "VATABLE", null, null, null, "SN-001")],
            [new CustomerPaymentTenderInput(paymentMethodId, 100, null, null)], "cashier");

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Customer_Given_And_No_Walk_In_Customer_Configured()
    {
        var (db, branchId, terminalId, _, itemId, uomId, _, paymentMethodId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.WalkInCustomerNotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Default_To_Walk_In_Customer_When_None_Given()
    {
        var (db, branchId, terminalId, _, itemId, uomId, _, paymentMethodId) = await Seed();
        var walkIn = LoanTestFixtures.Customer(branchId, name: "Walk-in Customer");
        db.Customers.Add(walkIn);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId: null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Payment_Method_Not_Found()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, terminalId, itemId, uomId, Guid.NewGuid(), customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.PaymentMethodNotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Invoice_Requires_Manager_Approval()
    {
        var (db, branchId, terminalId, customerId, itemId, uomId, _, paymentMethodId) = await Seed();
        var draftInvoiceResponse = new SalesInvoiceResponse(Guid.NewGuid(), "SINV-0001", branchId, customerId, "Test Customer", null,
            DateTimeOffset.UtcNow, null, "DRAFT", null, null, null, 0, 0, 100, null, null, [], null, null, null, DateTimeOffset.UtcNow, "cashier", 0, null, null, []);
        var fakeInvoiceHandler = LoanTestFixtures.SuccessHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>>(Result.Success(draftInvoiceResponse));

        var result = await Handler(db, createSalesInvoiceHandler: fakeInvoiceHandler)
            .HandleAsync(Command(branchId, terminalId, itemId, uomId, paymentMethodId, customerId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PosSale.RequiresApproval");
        await db.DisposeAsync();
    }
}
