namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanWriteOffs.Approve;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The success path calls `ExecuteUpdateAsync` for the trailing status flips after
/// PostLoanLedgerEntryCommand's ChangeTracker.Clear() — EF Core's InMemory provider doesn't support
/// ExecuteUpdate at all, so only the guard clauses before that point are covered here (see
/// LoanTestFixtures' doc comment).
/// </summary>
public sealed class ApproveLoanWriteOffCommandHandlerTests
{
    private static ApproveLoanWriteOffCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success(new GlJournalResponse(Guid.NewGuid(), "JV-0001", "br-1", DateTimeOffset.UtcNow, "LOAN", "LoanWriteOff", "x", null, "POSTED", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>(Result.Success(new PostLoanLedgerEntryResponse(Guid.NewGuid(), 0m))),
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanWriteOff writeOff)> Seed(
        string writeOffStatus = "PENDING_APPROVAL", string accountStatus = "ACTIVE", bool withApprovalRequest = true,
        bool withReceivableAccount = true, bool withLedgerBalance = true)
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
        var receivableAccount = LoanTestFixtures.GlAccount(code: "1350", name: "Loans Receivable");
        db.GlAccounts.Add(receivableAccount);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus,
            loanReceivableAccountId: withReceivableAccount ? receivableAccount.Id : null, withSchedule: false);
        db.LoanAccounts.Add(account);
        if (withLedgerBalance)
        {
            db.LoanLedgerEntries.Add(new LoanLedgerEntry
            {
                LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
                ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
                RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
            });
        }
        var writeOff = new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-0001", BranchId = branch.Id, LoanAccountId = account.Id, WriteOffDate = DateTimeOffset.UtcNow,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccount.Id, Reason = "x", Status = writeOffStatus
        };
        db.LoanWriteOffs.Add(writeOff);
        if (withApprovalRequest)
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_WRITE_OFF", EntityId = writeOff.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, writeOff);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(Guid.NewGuid(), "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_HqApprovalAuthority()
    {
        var (db, writeOff) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasHqApprovalAuthorityAsync("LOAN_WRITE_OFFS", Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, writeOff) = await Seed(writeOffStatus: "DRAFT", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_No_Longer_Active()
    {
        var (db, writeOff) = await Seed(accountStatus: "FULLY_PAID");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.AccountNotActive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Receivable_Account_Configured()
    {
        var (db, writeOff) = await Seed(withReceivableAccount: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, writeOff) = await Seed(withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Balance_Left_To_Write_Off()
    {
        var (db, writeOff) = await Seed(withLedgerBalance: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanWriteOffCommand(writeOff.Id, "hqadmin", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NoBalance");
        await db.DisposeAsync();
    }
}
