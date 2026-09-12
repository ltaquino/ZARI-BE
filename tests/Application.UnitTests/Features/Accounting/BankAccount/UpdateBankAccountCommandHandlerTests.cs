namespace ZARI.Application.UnitTests.Features.Accounting.BankAccount;

using ZARI.Application.Features.Accounting.BankAccounts.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateBankAccountCommandHandlerTests
{
    private static UpdateBankAccountCommand Command(Guid id, string branchId, Guid glAccountId) =>
        new(id, branchId, "Updated Account", "999", "Updated Bank", glAccountId, null);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid glAccountId, ZARI.Domain.Entities.BankAccount bankAccount)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var glAccount = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(glAccount);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, glAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, glAccount.Id, bankAccount);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_BankAccount()
    {
        var (db, branchId, glAccountId, bankAccount) = await Seed();
        var handler = new UpdateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(bankAccount.Id, branchId, glAccountId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.BankAccounts.FindAsync([bankAccount.Id], TestContext.Current.CancellationToken))!.AccountName.Should().Be("Updated Account");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, branchId, glAccountId, _) = await Seed();
        var handler = new UpdateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId, glAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, glAccountId, bankAccount) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateBankAccountCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(bankAccount.Id, branchId, glAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
