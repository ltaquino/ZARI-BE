namespace ZARI.Application.UnitTests.Features.Accounting.GlAccount;

using ZARI.Application.Features.Accounting.GlAccounts.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateGlAccountCommandHandlerTests
{
    private static CreateGlAccountCommand Command(string code = "2000", Guid? parentAccountId = null) =>
        new(code, "Accounts Payable", "Liability", "Credit", parentAccountId, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_GlAccount()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("2000");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_ACCOUNTS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateGlAccountCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.GlAccounts.Add(LoanTestFixtures.GlAccount(code: "2000"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_With_Parent_Account()
    {
        await using var db = TestDbContextFactory.Create();
        var parent = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(parent);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(code: "2001", parentAccountId: parent.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParentAccountId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Parent_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(parentAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.ParentNotFound");
    }
}
