namespace ZARI.Application.UnitTests.Features.Accounting.CostCenter;

using ZARI.Application.Features.Accounting.CostCenters.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllCostCentersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_CostCenters()
    {
        await using var db = TestDbContextFactory.Create();
        db.CostCenters.AddRange(LoanTestFixtures.CostCenter(code: "CC1"), LoanTestFixtures.CostCenter(code: "CC2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllCostCentersQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCostCentersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("COST_CENTERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllCostCentersQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllCostCentersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
