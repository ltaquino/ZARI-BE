namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Loan.LoanWriteOffs.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateLoanWriteOffCommandHandlerTests
{
    private static UpdateLoanWriteOffCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_WRITE_OFF", "x", "br-1", "UPDATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanWriteOff writeOff, string branchId, Guid expenseAccountId)> Seed(string status = "DRAFT")
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
        db.LoanLedgerEntries.Add(new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
            RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        });
        var writeOff = new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-0001", BranchId = branch.Id, LoanAccountId = account.Id, WriteOffDate = DateTimeOffset.UtcNow,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccount.Id, Reason = "x", Status = status
        };
        db.LoanWriteOffs.Add(writeOff);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, writeOff, branch.Id, expenseAccount.Id);
    }

    private static UpdateLoanWriteOffCommand Command(Guid id, string branchId, Guid expenseAccountId) =>
        new(id, branchId, DateTimeOffset.UtcNow, expenseAccountId, "updated reason", null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Update_When_Draft()
    {
        var (db, writeOff, branchId, expenseAccountId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(writeOff.Id, branchId, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Reason.Should().Be("updated reason");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, _, branchId, expenseAccountId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, writeOff, branchId, expenseAccountId) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(writeOff.Id, branchId, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, writeOff, branchId, expenseAccountId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(writeOff.Id, branchId, expenseAccountId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
