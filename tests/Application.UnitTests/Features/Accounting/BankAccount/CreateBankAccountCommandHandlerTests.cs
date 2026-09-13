namespace ZARI.Application.UnitTests.Features.Accounting.BankAccount;

using ZARI.Application.Features.Accounting.BankAccounts.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateBankAccountCommandHandlerTests
{
    private static CreateBankAccountCommand Command(string branchId, Guid glAccountId, string? currencyId = null) =>
        new(branchId, "Main Account", "1234567890", "Test Bank", glAccountId, currencyId);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid glAccountId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var glAccount = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, glAccount.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_BankAccount()
    {
        var (db, branchId, glAccountId) = await Seed();
        var handler = new CreateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId, glAccountId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountName.Should().Be("Main Account");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, glAccountId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("BANK_ACCOUNTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateBankAccountCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, glAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, glAccountId) = await Seed();
        var handler = new CreateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing", glAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_GlAccount_Not_Found()
    {
        var (db, branchId, _) = await Seed();
        var handler = new CreateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Not_Found()
    {
        var (db, branchId, glAccountId) = await Seed();
        var handler = new CreateBankAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId, glAccountId) with { CurrencyId = "cur-missing" }, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.NotFound");
        await db.DisposeAsync();
    }
}
