namespace ZARI.Application.UnitTests.Features.Loan.LoanLedgerEntry;

using ZARI.Application.Features.Loan.LoanLedgerEntries.Post;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// This handler locks the LoanAccount row with a raw MySQL `FOR UPDATE` query and runs inside a
/// `CreateExecutionStrategy` + `BeginTransactionAsync` unit of work — none of that is supported by
/// EF Core's InMemory provider (FromSqlInterpolated against a MySQL-only syntax, and the InMemory
/// provider has no real transaction/locking model). So only the two guard clauses that run on
/// plain LINQ *before* that block — the LoanAccount not-found check and the idempotency
/// short-circuit — are covered here. The actual insert/balance-roll-forward path is covered by
/// this session's extensive live/API-level curl testing of the Loan module instead.
/// </summary>
public sealed class PostLoanLedgerEntryCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_LoanAccount_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new PostLoanLedgerEntryCommandHandler(db);

        var result = await handler.HandleAsync(
            new PostLoanLedgerEntryCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "DISBURSEMENT", "LoanDisbursement", "x", 1000, 0, "desc", false),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Existing_Entry_When_Already_Posted_In_Same_Direction()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        var existing = new LoanLedgerEntry
        {
            LoanAccountId = account.Id, SequenceNo = 1, EntryDate = DateTimeOffset.UtcNow, TransactionType = "DISBURSEMENT",
            ReferenceTable = "LoanDisbursement", ReferenceId = "ref-1", PrincipalIn = 12000, PrincipalOut = 0,
            RunningPrincipalBalance = 12000, IsReversal = false, PostedAt = DateTimeOffset.UtcNow
        };
        db.LoanLedgerEntries.Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new PostLoanLedgerEntryCommandHandler(db);

        var result = await handler.HandleAsync(
            new PostLoanLedgerEntryCommand(account.Id, DateTimeOffset.UtcNow, "DISBURSEMENT", "LoanDisbursement", "ref-1", 12000, 0, "retry", false),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        result.Value.RunningPrincipalBalance.Should().Be(12000);
    }
}
