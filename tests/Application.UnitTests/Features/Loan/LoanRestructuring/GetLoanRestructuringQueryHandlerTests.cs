namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetLoanRestructuringQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanRestructuring restructuring)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        var restructuring = new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-0001", BranchId = branch.Id, OldLoanAccountId = account.Id, RestructureDate = DateTimeOffset.UtcNow,
            NewAnnualInterestRatePct = 10, NewTermMonths = 12, NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2,
            NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1), Reason = "x", Status = "DRAFT"
        };
        db.LoanRestructurings.Add(restructuring);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, restructuring);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Restructuring_When_Found()
    {
        var (db, restructuring) = await Seed();
        var handler = new GetLoanRestructuringQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanRestructuringQuery(restructuring.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RestructuringNo.Should().Be("LOAN-RESTR-0001");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLoanRestructuringQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanRestructuringQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, restructuring) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.View, restructuring.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLoanRestructuringQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetLoanRestructuringQuery(restructuring.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
