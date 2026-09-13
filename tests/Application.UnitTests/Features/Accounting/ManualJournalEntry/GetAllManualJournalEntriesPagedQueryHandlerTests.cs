namespace ZARI.Application.UnitTests.Features.Accounting.ManualJournalEntry;

using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllManualJournalEntriesPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Entries()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        for (var i = 0; i < 3; i++) db.ManualJournalEntries.Add(AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllManualJournalEntriesPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllManualJournalEntriesPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search_On_EntryNo_Or_Remarks()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var debit = LoanTestFixtures.GlAccount(code: "6000");
        var credit = LoanTestFixtures.GlAccount(code: "2000");
        db.GlAccounts.AddRange(debit, credit);
        var entry = AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id);
        entry.Remarks = "Depreciation accrual for March";
        db.ManualJournalEntries.Add(entry);
        db.ManualJournalEntries.Add(AccountingTestFixtures.ManualJournalEntry(branch.Id, debit.Id, credit.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllManualJournalEntriesPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllManualJournalEntriesPagedQuery(Page: 1, PageSize: 20, Search: "Depreciation"), TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("MANUAL_JOURNAL_ENTRIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllManualJournalEntriesPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllManualJournalEntriesPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
