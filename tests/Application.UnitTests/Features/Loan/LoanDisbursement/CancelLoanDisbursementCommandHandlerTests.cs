namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Loan.LoanDisbursements.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CancelLoanDisbursementCommandHandlerTests
{
    private static CancelLoanDisbursementCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "CANCELLED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var glAccount = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(glAccount);
        var paymentMethod = LoanTestFixtures.PaymentMethod(glAccount.Id);
        db.PaymentMethods.Add(paymentMethod);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, loanReceivableAccountId: glAccount.Id);
        db.LoanAccounts.Add(account);
        var disbursement = new LoanDisbursement
        {
            DisbursementNo = "LOAN-DISB-0001", BranchId = branch.Id, LoanAccountId = account.Id, DisbursementDate = DateTimeOffset.UtcNow,
            Amount = account.PrincipalAmount, PaymentMethodId = paymentMethod.Id, Status = status
        };
        db.LoanDisbursements.Add(disbursement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, disbursement);
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_When_Draft()
    {
        var (db, disbursement) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanDisbursementCommand(disbursement.Id, "u1", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, disbursement) = await Seed(status: "CANCELLED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanDisbursementCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Posted_Requires_Cancellation_Request()
    {
        var (db, disbursement) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanDisbursementCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.RequiresCancellationRequest");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, disbursement) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Cancel, disbursement.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new CancelLoanDisbursementCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
