namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Loan.LoanWriteOffs.Reject;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectLoanWriteOffCommandHandlerTests
{
    private static RejectLoanWriteOffCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanWriteOff writeOff)> Seed(string status = "PENDING_APPROVAL")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var expenseAccount = LoanTestFixtures.GlAccount(code: "6910", name: "Loan Write-off Expense");
        db.GlAccounts.Add(expenseAccount);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        var writeOff = new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-0001", BranchId = branch.Id, LoanAccountId = account.Id, WriteOffDate = DateTimeOffset.UtcNow,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccount.Id, Reason = "x", Status = status
        };
        db.LoanWriteOffs.Add(writeOff);
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_WRITE_OFF", EntityId = writeOff.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, writeOff);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        var (db, writeOff) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanWriteOffCommand(writeOff.Id, "hqadmin", "fix this"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, writeOff) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanWriteOffCommand(writeOff.Id, "hqadmin", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_HqApprovalAuthority()
    {
        var (db, writeOff) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasHqApprovalAuthorityAsync("LOAN_WRITE_OFFS", Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new RejectLoanWriteOffCommand(writeOff.Id, "hqadmin", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
