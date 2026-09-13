namespace ZARI.Application.UnitTests.Features.Accounting.BankAccount;

using ZARI.Application.Features.Accounting.BankAccounts.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllBankAccountsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_BankAccounts()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var glAccount = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(glAccount);
        db.BankAccounts.Add(AccountingTestFixtures.BankAccount(branch.Id, glAccount.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllBankAccountsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllBankAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("BANK_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllBankAccountsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllBankAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
