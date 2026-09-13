namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetDocumentSequenceQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Sequence_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id);
        db.DocumentSequences.Add(sequence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetDocumentSequenceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDocumentSequenceQuery(sequence.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocType.Should().Be("SO");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetDocumentSequenceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDocumentSequenceQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id);
        db.DocumentSequences.Add(sequence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetDocumentSequenceQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetDocumentSequenceQuery(sequence.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
