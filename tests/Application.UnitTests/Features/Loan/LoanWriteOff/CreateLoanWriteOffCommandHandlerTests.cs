namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Loan.LoanWriteOffs.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLoanWriteOffCommandHandlerTests
{
    private static CreateLoanWriteOffCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-WO-0001"))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, LoanAccount account, Guid expenseAccountId)> Seed(string accountStatus = "ACTIVE", bool withLedgerBalance = true)
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
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus, withSchedule: false);
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, account, expenseAccount.Id);
    }

    private static CreateLoanWriteOffCommand Command(string branchId, Guid loanAccountId, Guid expenseAccountId) =>
        new(branchId, loanAccountId, DateTimeOffset.UtcNow, expenseAccountId, "uncollectible after 2 years", null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Create_Draft_WriteOff()
    {
        var (db, branchId, account, expenseAccountId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value.Amount.Should().Be(12000);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, account, expenseAccountId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, account.Id, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Active()
    {
        var (db, branchId, account, expenseAccountId) = await Seed(accountStatus: "FULLY_PAID");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.AccountNotActive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Balance()
    {
        var (db, branchId, account, expenseAccountId) = await Seed(withLedgerBalance: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NoBalance");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Expense_Account_Not_Found()
    {
        var (db, branchId, account, _) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_In_Progress()
    {
        var (db, branchId, account, expenseAccountId) = await Seed();
        db.LoanWriteOffs.Add(new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-EXIST", BranchId = branchId, LoanAccountId = account.Id, WriteOffDate = DateTimeOffset.UtcNow,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccountId, Reason = "x", Status = "DRAFT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.AlreadyExists");
        await db.DisposeAsync();
    }
}
