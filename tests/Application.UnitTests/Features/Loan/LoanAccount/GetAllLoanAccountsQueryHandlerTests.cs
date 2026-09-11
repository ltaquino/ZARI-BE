namespace ZARI.Application.UnitTests.Features.Loan.LoanAccount;

using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllLoanAccountsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Accounts()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        db.LoanAccounts.Add(LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLoanAccountsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLoanAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllLoanAccountsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllLoanAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
