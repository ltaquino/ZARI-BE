namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.Features.Loan.LoanPayments.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The success path ends with an unconditional `ExecuteUpdateAsync` call to flip the payment to
/// POSTED (and the account to FULLY_PAID when applicable) after PostLoanLedgerEntryCommand's
/// ChangeTracker.Clear() — EF Core's InMemory provider doesn't support ExecuteUpdate at all, so
/// only the guard clauses before that point are covered here (see LoanTestFixtures' doc comment).
/// That still covers every business rule this handler enforces; the full posting path is covered
/// by this session's live curl testing.
/// </summary>
public sealed class CreateLoanPaymentCommandHandlerTests
{
    private static CreateLoanPaymentCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-PMT-0001"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success(new GlJournalResponse(Guid.NewGuid(), "JV-0001", "br-1", DateTimeOffset.UtcNow, "LOAN", "LoanPayment", "x", null, "POSTED", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostLoanLedgerEntryCommand, Result<PostLoanLedgerEntryResponse>>(Result.Success(new PostLoanLedgerEntryResponse(Guid.NewGuid(), 0m))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_PAYMENT", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, LoanAccount account, Guid paymentMethodId)> Seed(
        string accountStatus = "ACTIVE", bool withReceivableAccount = true)
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
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus,
            loanReceivableAccountId: withReceivableAccount ? glAccount.Id : null,
            interestIncomeAccountId: withReceivableAccount ? glAccount.Id : null,
            penaltyIncomeAccountId: withReceivableAccount ? glAccount.Id : null);
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, account, paymentMethod.Id);
    }

    private static CreateLoanPaymentCommand Command(string branchId, Guid loanAccountId, Guid paymentMethodId, decimal amount) =>
        new(branchId, loanAccountId, DateTimeOffset.UtcNow, amount, paymentMethodId, null, null, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_PAYMENTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Active()
    {
        var (db, branchId, account, paymentMethodId) = await Seed(accountStatus: "PENDING_DISBURSEMENT");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanPayment.AccountNotActive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Found()
    {
        var (db, branchId, _, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, Guid.NewGuid(), paymentMethodId, 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Exceeds_Outstanding()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, 999999), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanPayment.AmountExceedsOutstanding");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Receivable_Account_Configured()
    {
        var (db, branchId, account, paymentMethodId) = await Seed(withReceivableAccount: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
