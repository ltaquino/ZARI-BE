namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllLoanRestructuringsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Restructurings()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        db.LoanRestructurings.Add(new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-0001", BranchId = branch.Id, OldLoanAccountId = account.Id, RestructureDate = DateTimeOffset.UtcNow,
            NewAnnualInterestRatePct = 10, NewTermMonths = 12, NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2,
            NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1), Reason = "x", Status = "DRAFT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLoanRestructuringsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLoanRestructuringsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_RESTRUCTURINGS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllLoanRestructuringsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllLoanRestructuringsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
