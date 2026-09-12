namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseRequest;

using ZARI.Application.Features.Purchasing.PurchaseRequests.Create;
using ZARI.Application.Features.Purchasing.PurchaseRequests.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdatePurchaseRequestCommandHandlerTests
{
    private static UpdatePurchaseRequestCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdatePurchaseRequestCommand Command(Guid id, string branchId, Guid itemId, Guid uomId, decimal qty = 20) =>
        new(id, branchId, DateTimeOffset.UtcNow, "Updated remarks", "encoder", [new PurchaseRequestLineInput(itemId, qty, uomId, null)]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid itemId, Guid uomId, ZARI.Domain.Entities.PurchaseRequest request)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = PurchasingTestFixtures.PurchaseRequest(branch.Id, item.Id, uom.Id, status: status);
        db.PurchaseRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, item.Id, uom.Id, request);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Request()
    {
        var (db, branchId, itemId, uomId, request) = await Seed();

        var result = await Handler(db).HandleAsync(Command(request.Id, branchId, itemId, uomId, 25), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().Contain(l => l.QtyRequested == 25);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, branchId, itemId, uomId, _) = await Seed();

        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid(), branchId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, itemId, uomId, request) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(Command(request.Id, branchId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, branchId, itemId, uomId, request) = await Seed(status: "APPROVED");

        var result = await Handler(db).HandleAsync(Command(request.Id, branchId, itemId, uomId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseRequest.NotDraft");
        await db.DisposeAsync();
    }
}
