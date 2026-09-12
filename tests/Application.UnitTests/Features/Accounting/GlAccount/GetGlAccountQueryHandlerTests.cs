namespace ZARI.Application.UnitTests.Features.Accounting.GlAccount;

using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetGlAccountQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_GlAccount_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGlAccountQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGlAccountQuery(account.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(account.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetGlAccountQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGlAccountQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetGlAccountQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetGlAccountQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
