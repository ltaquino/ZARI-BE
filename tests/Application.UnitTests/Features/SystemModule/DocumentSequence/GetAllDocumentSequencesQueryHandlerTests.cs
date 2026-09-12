namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllDocumentSequencesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Sequences()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.DocumentSequences.AddRange(
            SystemModuleTestFixtures.DocumentSequence(branch.Id, docType: "SO"),
            SystemModuleTestFixtures.DocumentSequence(branch.Id, docType: "PO"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllDocumentSequencesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllDocumentSequencesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DOCUMENT_SEQUENCES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllDocumentSequencesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllDocumentSequencesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
