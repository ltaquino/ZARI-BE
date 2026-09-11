namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanPayments.ApproveCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The success path calls `ExecuteUpdateAsync` for the final payment status flip after
/// PostLoanLedgerEntryCommand's ChangeTracker.Clear() — EF Core's InMemory provider doesn't support
/// ExecuteUpdate at all, so only the guard clauses before that point are covered here (see
/// LoanTestFixtures' doc comment).
/// </summary>
public sealed class ApproveLoanPaymentCancellationCommandHandlerTests
{
    private static ApproveLoanPaymentCancellationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>>(Result.Success(new List<GlJournalResponse>())),
            LoanTestFixtures.SuccessHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>(Result.Success(new PostLoanLedgerEntryResponse(Guid.NewGuid(), 0m))),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_PAYMENT", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_PAYMENT", "x", "br-1", "CANCELLATION_APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanPayment payment)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
        var payment = new LoanPayment
        {
            PaymentNo = "LOAN-PMT-0001", BranchId = branch.Id, LoanAccountId = account.Id, PaymentDate = DateTimeOffset.UtcNow,
            Amount = 500, PaymentMethodId = paymentMethod.Id, Status = status
        };
        db.LoanPayments.Add(payment);
        if (withApprovalRequest)
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_PAYMENT", EntityId = payment.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanPaymentCancellationCommand(Guid.NewGuid(), "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_HqCancellationAuthority()
    {
        var (db, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("LOAN_PAYMENTS", Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new ApproveLoanPaymentCancellationCommand(payment.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, payment) = await Seed(status: "POSTED", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanPaymentCancellationCommand(payment.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanPayment.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, payment) = await Seed(withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanPaymentCancellationCommand(payment.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
