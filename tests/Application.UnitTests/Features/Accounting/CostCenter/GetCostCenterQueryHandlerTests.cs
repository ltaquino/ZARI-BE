namespace ZARI.Application.UnitTests.Features.Accounting.CostCenter;

using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetCostCenterQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_CostCenter_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter();
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCostCenterQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCostCenterQuery(costCenter.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(costCenter.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCostCenterQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCostCenterQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter();
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("COST_CENTERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCostCenterQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCostCenterQuery(costCenter.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
