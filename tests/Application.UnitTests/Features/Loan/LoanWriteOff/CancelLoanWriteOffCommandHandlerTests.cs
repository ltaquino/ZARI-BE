namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Loan.LoanWriteOffs.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CancelLoanWriteOffCommandHandlerTests
{
    private static CancelLoanWriteOffCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "CANCELLED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanWriteOff writeOff)> Seed(string status = "DRAFT")
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, writeOff);
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_When_Draft()
    {
        var (db, writeOff) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanWriteOffCommand(writeOff.Id, "u1", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, writeOff) = await Seed(status: "CANCELLED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanWriteOffCommand(writeOff.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Posted_Requires_Cancellation_Request()
    {
        var (db, writeOff) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanWriteOffCommand(writeOff.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.RequiresCancellationRequest");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, writeOff) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Cancel, writeOff.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new CancelLoanWriteOffCommand(writeOff.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
