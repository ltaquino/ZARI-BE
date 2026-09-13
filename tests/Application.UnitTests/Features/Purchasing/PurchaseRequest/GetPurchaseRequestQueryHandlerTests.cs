namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseRequest;

using ZARI.Application.Features.Purchasing.PurchaseRequests.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetPurchaseRequestQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.PurchaseRequest request)> Seed()
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
        var request = PurchasingTestFixtures.PurchaseRequest(branch.Id, item.Id, uom.Id);
        db.PurchaseRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Request_When_Found()
    {
        var (db, request) = await Seed();
        var handler = new GetPurchaseRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPurchaseRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseRequestQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, request) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.View, request.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPurchaseRequestQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPurchaseRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
