namespace ZARI.Application.UnitTests.Features.Loan.LoanRestructuring;

using ZARI.Application.Features.Loan.LoanRestructurings.Reject;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectLoanRestructuringCommandHandlerTests
{
    private static RejectLoanRestructuringCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_RESTRUCTURING", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanRestructuring restructuring)> Seed(string status = "PENDING_APPROVAL")
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
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_RESTRUCTURING", EntityId = restructuring.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, restructuring);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        var (db, restructuring) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCommand(restructuring.Id, "approver1", "fix this"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, restructuring) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCommand(restructuring.Id, "approver1", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanRestructuring.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, restructuring) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_RESTRUCTURINGS", FormAction.Approve, restructuring.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new RejectLoanRestructuringCommand(restructuring.Id, "approver1", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
