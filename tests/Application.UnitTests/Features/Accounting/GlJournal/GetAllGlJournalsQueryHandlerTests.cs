namespace ZARI.Application.UnitTests.Features.Accounting.GlJournal;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllGlJournalsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Journals_With_Lines()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var account = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.GlJournals.Add(new GlJournal
        {
            JournalNo = "JV-0001", BranchId = branch.Id, JournalDate = DateTimeOffset.UtcNow, SourceModule = "TEST",
            SourceReferenceTable = "TestDocument", SourceReferenceId = "doc-1", Status = "POSTED",
            Lines = [new GlJournalLine { AccountId = account.Id, DebitAmount = 100, CreditAmount = 0 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGlJournalsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGlJournalsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GL_JOURNALS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllGlJournalsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllGlJournalsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
