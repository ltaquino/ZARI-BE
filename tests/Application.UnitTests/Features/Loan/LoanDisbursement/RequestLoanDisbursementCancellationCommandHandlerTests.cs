namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Loan.LoanDisbursements.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RequestLoanDisbursementCancellationCommandHandlerTests
{
    private static RequestLoanDisbursementCancellationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "u1", DateTimeOffset.UtcNow, "PENDING", "CANCEL", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "CANCELLATION_REQUESTED", "APPROVAL_NEEDED", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement, LoanAccount account)> Seed(string status = "POSTED", bool withPayment = false)
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
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: "ACTIVE", loanReceivableAccountId: glAccount.Id);
        db.LoanAccounts.Add(account);
        var disbursement = new LoanDisbursement
        {
            DisbursementNo = "LOAN-DISB-0001", BranchId = branch.Id, LoanAccountId = account.Id, DisbursementDate = DateTimeOffset.UtcNow,
            Amount = account.PrincipalAmount, PaymentMethodId = paymentMethod.Id, Status = status
        };
        db.LoanDisbursements.Add(disbursement);
        if (withPayment)
        {
            db.LoanPayments.Add(new LoanPayment
            {
                PaymentNo = "LOAN-PMT-0001", BranchId = branch.Id, LoanAccountId = account.Id, PaymentDate = DateTimeOffset.UtcNow,
                Amount = 500, PaymentMethodId = paymentMethod.Id, Status = "POSTED"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, disbursement, account);
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation_When_Posted()
    {
        var (db, disbursement, _) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RequestLoanDisbursementCancellationCommand(disbursement.Id, "u1", "encoding error"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, disbursement, _) = await Seed(status: "DRAFT");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RequestLoanDisbursementCancellationCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.NotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Downstream_Payments()
    {
        var (db, disbursement, _) = await Seed(withPayment: true);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RequestLoanDisbursementCancellationCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.HasDownstreamActivity");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, disbursement, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Cancel, disbursement.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new RequestLoanDisbursementCancellationCommand(disbursement.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
