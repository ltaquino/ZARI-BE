namespace ZARI.Application.UnitTests.Features.SystemModule.Branch;

using ZARI.Application.Features.SystemModule.Branches.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// The IsHeadOffice=true path calls `ExecuteUpdateAsync` — unsupported by the InMemory provider,
/// same gap as CreateBranchCommandHandlerTests. Only IsHeadOffice=false is covered here.
/// </summary>
public sealed class UpdateBranchCommandHandlerTests
{
    private static UpdateBranchCommand Command(string id, string code = "EB", bool isHeadOffice = false) =>
        new(id, "Updated Name", code, "Cebu City", "New Address", "111-1111", "active", isHeadOffice, null, null, null, null, null);

    [Fact]
    public async Task HandleAsync_Should_Update_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Branches.FindAsync([branch.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing"), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("BRANCHES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateBranchCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts_With_Another_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        var other = LoanTestFixtures.Branch("br-2");
        db.Branches.AddRange(branch, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id, code: "BR-2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
