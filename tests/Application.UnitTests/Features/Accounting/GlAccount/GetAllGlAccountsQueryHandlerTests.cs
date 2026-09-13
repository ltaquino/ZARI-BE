namespace ZARI.Application.UnitTests.Features.Accounting.GlAccount;

using ZARI.Application.Features.Accounting.GlAccounts.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllGlAccountsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_GlAccounts()
    {
        await using var db = TestDbContextFactory.Create();
        db.GlAccounts.AddRange(LoanTestFixtures.GlAccount(code: "1000"), LoanTestFixtures.GlAccount(code: "2000"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGlAccountsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGlAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGlAccountsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGlAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
