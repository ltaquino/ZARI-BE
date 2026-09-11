namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateLoanRestructuringCommandHandlerTests
{
    private static UpdateLoanRestructuringCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "UPDATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanRestructuring restructuring, string branchId)> Seed(string status = "DRAFT")
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
            NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1), Reason = "x", Status = status
        };
        db.LoanRestructurings.Add(restructuring);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, restructuring, branch.Id);
    }

    private static UpdateLoanRestructuringCommand Command(Guid id, string branchId, decimal rate = 8) =>
        new(id, branchId, DateTimeOffset.UtcNow, rate, 12, "MONTHLY", 5, 2, DateTimeOffset.UtcNow.AddMonths(1), "updated reason", null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Update_When_Draft()
    {
        var (db, restructuring, branchId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(restructuring.Id, branchId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NewAnnualInterestRatePct.Should().Be(8);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, _, branchId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, restructuring, branchId) = await Seed(status: "PENDING_APPROVAL");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(restructuring.Id, branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, restructuring, branchId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(restructuring.Id, branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
