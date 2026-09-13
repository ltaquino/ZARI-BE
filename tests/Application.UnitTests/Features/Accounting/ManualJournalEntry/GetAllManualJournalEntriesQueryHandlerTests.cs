namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllManualJournalEntriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Entries()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        db.ManualJournalEntries.Add(AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllManualJournalEntriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllManualJournalEntriesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("MANUAL_JOURNAL_ENTRIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllManualJournalEntriesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllManualJournalEntriesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
