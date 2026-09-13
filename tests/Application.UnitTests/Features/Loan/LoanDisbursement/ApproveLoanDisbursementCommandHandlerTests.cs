namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanDisbursements.Approve;
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
/// full posting path is covered by this session's live curl testing instead.
/// </summary>
public sealed class ApproveLoanDisbursementCommandHandlerTests
{
    private static ApproveLoanDisbursementCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success(new GlJournalResponse(Guid.NewGuid(), "JV-0001", "br-1", DateTimeOffset.UtcNow, "LOAN", "LoanDisbursement", "x", null, "POSTED", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>(Result.Success(new PostLoanLedgerEntryResponse(Guid.NewGuid(), 12000m))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement, LoanAccount account)> Seed(string disbursementStatus = "PENDING_APPROVAL", string accountStatus = "PENDING_DISBURSEMENT", bool withApprovalRequest = true, bool withReceivableAccount = true)
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
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus, loanReceivableAccountId: withReceivableAccount ? glAccount.Id : null);
        db.LoanAccounts.Add(account);
        var disbursement = new LoanDisbursement
        {
            DisbursementNo = "LOAN-DISB-0001", BranchId = branch.Id, LoanAccountId = account.Id, DisbursementDate = DateTimeOffset.UtcNow,
            Amount = account.PrincipalAmount, PaymentMethodId = paymentMethod.Id, Status = disbursementStatus
        };
        db.LoanDisbursements.Add(disbursement);
        if (withApprovalRequest)
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_DISBURSEMENT", EntityId = disbursement.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, disbursement, account);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(Guid.NewGuid(), "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, disbursement, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Approve, disbursement.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(disbursement.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, disbursement, _) = await Seed(disbursementStatus: "DRAFT", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(disbursement.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_No_Longer_Pending_Disbursement()
    {
        var (db, disbursement, _) = await Seed(accountStatus: "CANCELLED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(disbursement.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AccountNotPendingDisbursement");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Receivable_Account_Configured()
    {
        var (db, disbursement, _) = await Seed(withReceivableAccount: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(disbursement.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, disbursement, _) = await Seed(withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanDisbursementCommand(disbursement.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
