namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseOrder;

using ZARI.Application.Features.Purchasing.PurchaseOrders.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePurchaseOrderCommandHandlerTests
{
    private static CreatePurchaseOrderCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new GetNextDocumentNumberCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreatePurchaseOrderCommand Command(string branchId, Guid supplierId, Guid itemId, Guid uomId, Guid? purchaseRequestId = null, Guid? purchaseRequestLineId = null, decimal qty = 5) =>
        new(branchId, supplierId, DateTimeOffset.UtcNow, null, "urgent", purchaseRequestId, "encoder", [new PurchaseOrderLineInput(itemId, qty, uomId, 50, purchaseRequestLineId)]);

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
    public async Task HandleAsync_Should_Create_Draft_Order()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_ORDERS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(branchId, supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command("br-missing", supplierId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Supplier_Not_Found()
    {
        var (db, branchId, _, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, Guid.NewGuid(), itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Supplier.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        var (db, branchId, supplierId, _, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, Guid.NewGuid(), uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Uom_Not_Found()
    {
        var (db, branchId, supplierId, itemId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Uom.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Purchase_Request_Not_Found()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: Guid.NewGuid(), purchaseRequestLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Purchase_Request_Not_Approved()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var pr = PurchasingTestFixtures.PurchaseRequest(branchId, itemId, uomId, status: "DRAFT");
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: pr.Id, purchaseRequestLineId: pr.Lines[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.PurchaseRequestNotApproved");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Line_Missing_Purchase_Request_Line()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var pr = PurchasingTestFixtures.PurchaseRequest(branchId, itemId, uomId, status: "APPROVED");
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: pr.Id, purchaseRequestLineId: null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.LineMissingPurchaseRequestLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Unexpected_Purchase_Request_Line()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: null, purchaseRequestLineId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.UnexpectedPurchaseRequestLine");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Exceeds_Requested_Qty()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var pr = PurchasingTestFixtures.PurchaseRequest(branchId, itemId, uomId, status: "APPROVED", qty: 10);
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: pr.Id, purchaseRequestLineId: pr.Lines[0].Id, qty: 20), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseOrder.ExceedsRequestedQty");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Order_Against_Approved_Purchase_Request()
    {
        var (db, branchId, supplierId, itemId, uomId) = await Seed();
        var pr = PurchasingTestFixtures.PurchaseRequest(branchId, itemId, uomId, status: "APPROVED", qty: 10);
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(Command(branchId, supplierId, itemId, uomId, purchaseRequestId: pr.Id, purchaseRequestLineId: pr.Lines[0].Id, qty: 5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PurchaseRequestId.Should().Be(pr.Id);
        await db.DisposeAsync();
    }
}
