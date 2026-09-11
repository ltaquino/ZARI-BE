namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Application.Features.Loan.LoanDisbursements.ApproveCancellation;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The success path (past the ApprovalRequest lookup) calls `ExecuteUpdateAsync` to flip
/// LoanDisbursement/LoanAccount status after PostLoanLedgerEntryCommand's ChangeTracker.Clear() —
/// EF Core's InMemory provider does not support ExecuteUpdate/ExecuteUpdateAsync at all (throws
/// InvalidOperationException), so only the guard clauses before that point are covered here; the
/// full reversal path is covered by this session's live curl testing instead.
/// </summary>
public sealed class ApproveLoanDisbursementCancellationCommandHandlerTests
{
    private static ApproveLoanDisbursementCancellationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>>(Result.Success(new List<GlJournalResponse>())),
            LoanTestFixtures.SuccessHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>(Result.Success(new PostLoanLedgerEntryResponse(Guid.NewGuid(), 0m))),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "CANCEL", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "CANCELLATION_APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement, LoanAccount account)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true, bool withPayment = false)
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
        if (withApprovalRequest)
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_DISBURSEMENT", EntityId = disbursement.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL" });
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
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, disbursement, _) = await Seed(status: "POSTED", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCancellationCommand(disbursement.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Downstream_Payments()
    {
        var (db, disbursement, _) = await Seed(withPayment: true);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCancellationCommand(disbursement.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.HasDownstreamActivity");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_HqCancellationAuthority()
    {
        var (db, disbursement, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("LOAN_DISBURSEMENTS", Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCancellationCommand(disbursement.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
