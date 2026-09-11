namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLoanRestructuringCommandHandlerTests
{
    private static CreateLoanRestructuringCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-RESTR-0001"))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, LoanAccount account)> Seed(string accountStatus = "ACTIVE", bool withOverdueUnpaidInstallment = false)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus, withSchedule: false);
        if (withOverdueUnpaidInstallment)
        {
            account.ScheduleLines.Add(new LoanAmortizationScheduleLine
            {
                InstallmentNo = 1, DueDate = DateTimeOffset.UtcNow.AddDays(-30), PrincipalDue = 1000, InterestDue = 100, TotalDue = 1100,
                OutstandingPrincipalAfter = 11000, PrincipalPaid = 0, InterestPaid = 0, PenaltyPaid = 0, Status = "DUE"
            });
        }
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, account);
    }

    private static CreateLoanRestructuringCommand Command(string branchId, Guid oldAccountId) =>
        new(branchId, oldAccountId, DateTimeOffset.UtcNow, 10, 12, "MONTHLY", 5, 2, DateTimeOffset.UtcNow.AddMonths(1), "hardship request", null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Restructuring()
    {
        var (db, branchId, account) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, account) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Active()
    {
        var (db, branchId, account) = await Seed(accountStatus: "FULLY_PAID");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.AccountNotActive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Arrears_Unsettled()
    {
        var (db, branchId, account) = await Seed(withOverdueUnpaidInstallment: true);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.ArrearsMustBeSettled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_In_Progress()
    {
        var (db, branchId, account) = await Seed();
        db.LoanRestructurings.Add(new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-EXIST", BranchId = branchId, OldLoanAccountId = account.Id, RestructureDate = DateTimeOffset.UtcNow,
            NewAnnualInterestRatePct = 10, NewTermMonths = 12, NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2,
            NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1), Reason = "x", Status = "DRAFT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.AlreadyExists");
        await db.DisposeAsync();
    }
}
