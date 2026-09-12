namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateDocumentSequenceCommandHandlerTests
{
    private static UpdateDocumentSequenceCommand Command(Guid id, string branchId, string docType = "SO") => new(id, branchId, docType, "SO-", 10, 6);

    [Fact]
    public async Task HandleAsync_Should_Update_Sequence()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id);
        db.DocumentSequences.Add(sequence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(sequence.Id, branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.DocumentSequences.FindAsync([sequence.Id], TestContext.Current.CancellationToken))!.NextNumber.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), "br-1"), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateDocumentSequenceCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(sequence.Id, branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Clashing_With_Another_Sequence()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id, docType: "SO");
        var other = SystemModuleTestFixtures.DocumentSequence(branch.Id, docType: "PO");
        db.DocumentSequences.AddRange(sequence, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(sequence.Id, branch.Id, docType: "PO"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var sequence = SystemModuleTestFixtures.DocumentSequence(branch.Id);
        db.DocumentSequences.Add(sequence);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(sequence.Id, "br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}
