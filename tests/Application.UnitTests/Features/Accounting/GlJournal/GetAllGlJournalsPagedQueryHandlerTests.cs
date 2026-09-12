namespace ZARI.Application.UnitTests.Features.Accounting.GlJournal;

using ZARI.Application.Features.Accounting.GlJournals.GetAllPaged;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllGlJournalsPagedQueryHandlerTests
{
    private static async Task<ZARI.Infrastructure.Persistence.AppDbContext> SeedJournals(int count)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        for (var i = 1; i <= count; i++)
        {
            db.GlJournals.Add(new GlJournal
            {
                JournalNo = $"JV-{i:0000}", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow.AddDays(-i), SourceModule = "TEST",
                SourceReferenceTable = "TestDocument", SourceReferenceId = $"doc-{i}", Status = "POSTED",
                Lines = [new GlJournalLine { AccountId = account.Id, DebitAmount = 100, CreditAmount = 0 }]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return db;
    }

    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Journals()
    {
        await using var db = await SeedJournals(5);
        var handler = new GetAllGlJournalsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGlJournalsPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search()
    {
        await using var db = await SeedJournals(3);
        var handler = new GetAllGlJournalsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGlJournalsPagedQuery(Page: 1, PageSize: 20, Search: "JV-0002"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(j => j.JournalNo == "JV-0002");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_JOURNALS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGlJournalsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGlJournalsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
