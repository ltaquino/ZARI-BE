namespace ZARI.Application.UnitTests.Features.SystemModule.Branch;

using ZARI.Application.Features.SystemModule.Branches.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// The IsHeadOffice=true path calls `ExecuteUpdateAsync` to demote every other branch — EF Core's
/// InMemory provider does not support ExecuteUpdate/ExecuteUpdateAsync at all (throws
/// InvalidOperationException), so only the IsHeadOffice=false path is covered here (same gap
/// documented on LoanTestFixtures for the Loan module's own Approve handlers).
/// </summary>
public sealed class CreateBranchCommandHandlerTests
{
    private static CreateBranchCommand Command(string code = "EB", bool isHeadOffice = false) =>
        new("East Branch", code, "Lapu-Lapu City", "Pusok", "000-0000", "active", isHeadOffice);

    [Fact]
    public async Task HandleAsync_Should_Create_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("EB");
        db.Branches.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("BRANCHES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateBranchCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch("br-existing"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(code: "BR-EXISTING"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
