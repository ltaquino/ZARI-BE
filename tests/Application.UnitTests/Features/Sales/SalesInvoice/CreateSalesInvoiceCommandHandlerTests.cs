namespace ZARI.Application.UnitTests.Features.Sales.SalesInvoice;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateSalesInvoiceCommandHandlerTests
{
    private static CreateSalesInvoiceCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new PostGlJournalCommandHandler(db, new GetNextDocumentNumberCommandHandler(db)),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateSalesInvoiceCommand Command(string branchId, Guid customerId, Guid itemId, Guid uomId, decimal discountPct = 0, Guid? statutoryDiscountTypeId = null, string? statutoryIdNumber = null, Guid? deliveryOrderId = null, Guid? deliveryOrderLineId = null, decimal qty = 1) =>
        new(branchId, customerId, deliveryOrderId, DateTimeOffset.UtcNow, null, "receipt", null, null, "encoder",
            [new SalesInvoiceLineInput(itemId, qty, uomId, 100, discountPct, null, null, "VATABLE", statutoryDiscountTypeId, statutoryIdNumber, deliveryOrderLineId)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid customerId, Guid itemId, Guid uomId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var arGlAccount = LoanTestFixtures.GlAccount(code: "1200", name: "Accounts Receivable", accountType: "Asset", normalBalance: "Debit");
        var revenueGlAccount = LoanTestFixtures.GlAccount(code: "4000", name: "Sales Revenue", accountType: "Revenue", normalBalance: "Credit");
        var vatGlAccount = LoanTestFixtures.GlAccount(code: "2200", name: "VAT Payable", accountType: "Liability", normalBalance: "Credit");
        db.GlAccounts.AddRange(arGlAccount, revenueGlAccount, vatGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, customer.Id, item.Id, uom.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Invoice()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        var (db, branchId, _, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branchId, customerId, _, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, Guid.NewGuid(), uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, branchId, customerId, itemId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Statutory_Discount_Type_Not_Found()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, statutoryDiscountTypeId: Guid.NewGuid(), statutoryIdNumber: "ID-1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StatutoryDiscountType.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Cost_Center_Not_Found()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var command = Command(branchId, customerId, itemId, uomId) with { CostCenterId = Guid.NewGuid() };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CostCenter.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Discount_Not_Allowed_For_Item()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, discountPct: 10), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.DiscountNotAllowedForItem");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_Discount_When_An_Active_Rule_Covers_The_Item()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var rule = SalesTestFixtures.DiscountRule(scope: "ALL");
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, discountPct: 10), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_Statutory_Discount_Without_An_Active_Rule()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var statutoryType = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(statutoryType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, statutoryDiscountTypeId: statutoryType.Id, statutoryIdNumber: "SC-001"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Single().VatType.Should().Be("VAT_EXEMPT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Delivery_Order_Not_Found()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, deliveryOrderId: Guid.NewGuid(), deliveryOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Delivery_Order_Not_Posted()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouse.Id, customerId, itemId, uomId, status: "DRAFT");
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.DeliveryOrderNotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Delivery_Order_Line()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, deliveryOrderId: null, deliveryOrderLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.UnexpectedDeliveryOrderLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Delivered_Qty()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouse.Id, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id, qty: 20), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.ExceedsDeliveredQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Invoice_Against_Posted_Delivery()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var warehouse = InventoryTestFixtures.Warehouse(branchId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var doOrder = SalesTestFixtures.DeliveryOrder(branchId, warehouse.Id, customerId, itemId, uomId, status: "POSTED", qty: 10);
        db.DeliveryOrders.Add(doOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId, deliveryOrderId: doOrder.Id, deliveryOrderLineId: doOrder.Lines[0].Id, qty: 5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeliveryOrderId.Should().Be(doOrder.Id);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Pos_Terminal_Not_Found()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var command = Command(branchId, customerId, itemId, uomId) with { PosTerminalId = Guid.NewGuid() };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SalesInvoice.PosTerminalNotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Quick_Post_When_Company_Toggle_Enabled_And_Post_A_Real_GlJournal()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var company = SystemModuleTestFixtures.Company();
        company.SalesInvoiceQuickPostEnabled = true;
        db.Companies.Add(company);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, customerId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        result.Value!.BirOrSeriesNumber.Should().NotBeNullOrEmpty();
        db.GlJournals.Should().ContainSingle(j => j.SourceReferenceTable == "SalesInvoice");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Force_Quick_Post_When_Flag_Set_Regardless_Of_Company_Toggle()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var command = Command(branchId, customerId, itemId, uomId) with { ForceQuickPost = true };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Quick_Post_When_Discount_Exceeds_Threshold_Even_With_ForceQuickPost()
    {
        var (db, branchId, customerId, itemId, uomId) = await Seed();
        var rule = SalesTestFixtures.DiscountRule(scope: "ALL");
        db.DiscountRules.Add(rule);
        var company = SystemModuleTestFixtures.Company();
        company.MaxUnapprovedDiscountPct = 5;
        db.Companies.Add(company);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = Command(branchId, customerId, itemId, uomId, discountPct: 20) with { ForceQuickPost = true };

        var result = await Handler(db).HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }
}
