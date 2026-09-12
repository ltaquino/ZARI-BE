namespace ZARI.Application.UnitTests.Features.Accounting.GlAccount;

using ZARI.Application.Features.Accounting.GlAccounts.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateGlAccountCommandHandlerTests
{
    private static UpdateGlAccountCommand Command(Guid id, string code = "2000", Guid? parentAccountId = null) =>
        new(id, code, "Updated Name", "Liability", "Credit", parentAccountId, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_GlAccount()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(account.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.GlAccounts.FindAsync([account.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_ACCOUNTS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateGlAccountCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount(code: "2000");
        var other = LoanTestFixtures.GlAccount(code: "2001");
        db.GlAccounts.AddRange(account, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(account.Id, code: "2001"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Is_Its_Own_Parent()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(account.Id, parentAccountId: account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.InvalidParent");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Parent_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(account.Id, parentAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.ParentNotFound");
    }
}
