namespace ZARI.Application.UnitTests.Features.Accounting.GlAccount;

using ZARI.Application.Features.Accounting.GlAccounts.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteGlAccountCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_GlAccount()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(account.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.GlAccounts.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_ACCOUNTS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteGlAccountCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Children()
    {
        await using var db = TestDbContextFactory.Create();
        var parent = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.Add(parent);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var child = LoanTestFixtures.GlAccount(code: "2001");
        child.ParentAccountId = parent.Id;
        db.GlAccounts.Add(child);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(parent.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.HasChildren");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_TaxCodes()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        db.TaxCodes.Add(AccountingTestFixtures.TaxCode(glAccountId: account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.HasTaxCodes");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_BankAccounts()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.BankAccounts.Add(AccountingTestFixtures.BankAccount(branch.Id, account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.HasBankAccounts");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Suppliers()
    {
        await using var db = TestDbContextFactory.Create();
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        db.Suppliers.Add(new Supplier { Code = "SUP1", Name = "Test Supplier", ApAccountId = account.Id, Status = "active" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGlAccountCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGlAccountCommand(account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.HasSuppliers");
    }
}
