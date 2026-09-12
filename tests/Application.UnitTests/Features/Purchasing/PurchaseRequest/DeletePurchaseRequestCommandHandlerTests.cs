namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseRequest;

using ZARI.Application.Features.Purchasing.PurchaseRequests.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeletePurchaseRequestCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.PurchaseRequest request)> Seed(string status = "DRAFT")
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
        return (db, request);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Request()
    {
        var (db, request) = await Seed();
        var handler = new DeletePurchaseRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePurchaseRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.PurchaseRequests.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePurchaseRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePurchaseRequestCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, request) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Delete, request.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeletePurchaseRequestCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeletePurchaseRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, request) = await Seed(status: "APPROVED");
        var handler = new DeletePurchaseRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePurchaseRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseRequest.NotDraft");
        await db.DisposeAsync();
    }
}
