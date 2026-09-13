namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteDocumentSequenceCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Sequence()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id);
        db.DocumentSequences.Add(sequence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDocumentSequenceCommand(sequence.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.DocumentSequences.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDocumentSequenceCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Delete, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteDocumentSequenceCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteDocumentSequenceCommand(sequence.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
