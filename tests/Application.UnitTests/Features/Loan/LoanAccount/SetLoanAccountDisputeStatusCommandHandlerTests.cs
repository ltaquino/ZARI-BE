namespace ZARI.Application.UnitTests.Features.Loan.LoanAccount;

using ZARI.Application.Features.Loan.LoanAccounts.SetDisputeStatus;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class SetLoanAccountDisputeStatusCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanAccount account)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "ACTIVE");
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, account);
    }

    [Fact]
    public async Task HandleAsync_Should_Set_Dispute_Flag()
    {
        var (db, account) = await Seed();
        var handler = new SetLoanAccountDisputeStatusCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SetLoanAccountDisputeStatusCommand(account.Id, true, "member disputes the balance", "u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDisputed.Should().BeTrue();
        result.Value.DisputeNotes.Should().Be("member disputes the balance");
    }

    [Fact]
    public async Task HandleAsync_Should_Clear_Notes_When_Undisputed()
    {
        var (db, account) = await Seed();
        var handler = new SetLoanAccountDisputeStatusCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        await handler.HandleAsync(new SetLoanAccountDisputeStatusCommand(account.Id, true, "was disputed", "u1"), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(new SetLoanAccountDisputeStatusCommand(account.Id, false, null, "u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDisputed.Should().BeFalse();
        result.Value.DisputeNotes.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new SetLoanAccountDisputeStatusCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SetLoanAccountDisputeStatusCommand(Guid.NewGuid(), true, "x", "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, account) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Edit, account.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SetLoanAccountDisputeStatusCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new SetLoanAccountDisputeStatusCommand(account.Id, true, "x", "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
