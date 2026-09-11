namespace ZARI.Application.UnitTests.Features.Loan.Reports.LoanDelinquency;

using ZARI.Application.Features.Loan.Reports.LoanDelinquency;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetLoanDelinquencyReportQueryHandlerTests
{
    private static async Task<ZARI.Infrastructure.Persistence.AppDbContext> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "ACTIVE", withSchedule: false);
        account.ScheduleLines.Add(new LoanAmortizationScheduleLine
        {
            InstallmentNo = 1, DueDate = DateTimeOffset.UtcNow.AddDays(-20), PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
            OutstandingPrincipalAfter = 11000, PrincipalPaid = 0, InterestPaid = 0, PenaltyPaid = 0, Status = "DUE"
        });
        account.ScheduleLines.Add(new LoanAmortizationScheduleLine
        {
            InstallmentNo = 2, DueDate = DateTimeOffset.UtcNow.AddDays(10), PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
            OutstandingPrincipalAfter = 10000, PrincipalPaid = 0, InterestPaid = 0, PenaltyPaid = 0, Status = "DUE"
        });
        account.ScheduleLines.Add(new LoanAmortizationScheduleLine
        {
            InstallmentNo = 3, DueDate = DateTimeOffset.UtcNow.AddDays(-50), PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
            OutstandingPrincipalAfter = 9000, PrincipalPaid = 1000, InterestPaid = 100, PenaltyPaid = 0, Status = "PAID"
        });
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return db;
    }

    [Fact]
    public async Task HandleAsync_Should_Only_Include_NonPaid_Lines()
    {
        await using var db = await Seed();
        var handler = new GetLoanDelinquencyReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Groups.Should().HaveCount(1);
        result.Value.Groups[0].Installments.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Bucket_By_DaysOverdue()
    {
        await using var db = await Seed();
        var handler = new GetLoanDelinquencyReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery(null, null, null), TestContext.Current.CancellationToken);

        var installments = result.Value!.Groups[0].Installments;
        installments.Should().ContainSingle(i => i.InstallmentNo == 1 && i.Bucket == "1-30");
        installments.Should().ContainSingle(i => i.InstallmentNo == 2 && i.Bucket == "current");
        result.Value.Days1To30.Should().BeGreaterThan(0);
        result.Value.Current.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_BranchId()
    {
        await using var db = await Seed();
        var handler = new GetLoanDelinquencyReportQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery("br-missing", null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLoanDelinquencyReportQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
