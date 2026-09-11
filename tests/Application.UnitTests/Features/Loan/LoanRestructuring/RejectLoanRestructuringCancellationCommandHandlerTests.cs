namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.RejectCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectLoanRestructuringCancellationCommandHandlerTests
{
    private static RejectLoanRestructuringCancellationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "CANCEL", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "CANCELLATION_REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanRestructuring restructuring)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var oldAccount = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "RESTRUCTURED", withSchedule: false);
        var newAccount = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "ACTIVE", withSchedule: false);
        db.LoanAccounts.Add(oldAccount);
        db.LoanAccounts.Add(newAccount);
        var restructuring = new LoanRestructuring
        {
            RestructuringNo = "LOAN-RESTR-0001", BranchId = branch.Id, OldLoanAccountId = oldAccount.Id, NewLoanAccountId = newAccount.Id, RestructureDate = DateTimeOffset.UtcNow,
            OldPrincipalBalance = 12000, NewAnnualInterestRatePct = 10, NewTermMonths = 12, NewRepaymentFrequency = "MONTHLY", NewGracePeriodDays = 5, NewPenaltyRatePct = 2,
            NewFirstDueDate = DateTimeOffset.UtcNow.AddMonths(1), Reason = "x", Status = status
        };
        db.LoanRestructurings.Add(restructuring);
        if (withApprovalRequest)
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_RESTRUCTURING", EntityId = restructuring.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, restructuring);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_And_Restore_Posted_Status()
    {
        var (db, restructuring) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCancellationCommand(restructuring.Id, "hqadmin", "not justified"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, restructuring) = await Seed(status: "POSTED", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCancellationCommand(restructuring.Id, "hqadmin", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_HqCancellationAuthority()
    {
        var (db, restructuring) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("LOAN_RESTRUCTURINGS", Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCancellationCommand(restructuring.Id, "hqadmin", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, restructuring) = await Seed(withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCancellationCommand(restructuring.Id, "hqadmin", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
