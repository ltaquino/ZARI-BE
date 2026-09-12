namespace ZARI.Application.UnitTests.Features.SystemModule.Branch;

using ZARI.Application.Features.SystemModule.Branches.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllBranchesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Branches_Ordered_By_Name()
    {
        await using var db = TestDbContextFactory.Create();
        var zBranch = LoanTestFixtures.Branch("br-z");
        zBranch.Name = "Zeta Branch";
        var aBranch = LoanTestFixtures.Branch("br-a");
        aBranch.Name = "Alpha Branch";
        db.Branches.AddRange(zBranch, aBranch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllBranchesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllBranchesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(b => b.Name).Should().ContainInOrder("Alpha Branch", "Zeta Branch");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("BRANCHES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllBranchesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllBranchesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
