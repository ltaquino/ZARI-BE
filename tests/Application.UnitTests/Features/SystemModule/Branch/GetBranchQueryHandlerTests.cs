namespace ZARI.Application.UnitTests.Features.SystemModule.Branch;

using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetBranchQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Branch_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetBranchQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetBranchQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(branch.Name);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetBranchQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetBranchQuery("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("BRANCHES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetBranchQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetBranchQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
