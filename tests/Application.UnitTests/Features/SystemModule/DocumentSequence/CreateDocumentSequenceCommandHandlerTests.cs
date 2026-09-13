namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateDocumentSequenceCommandHandlerTests
{
    private static CreateDocumentSequenceCommand Command(string branchId, string docType = "SO") => new(branchId, docType, "SO-", 1, 5);

    [Fact]
    public async Task HandleAsync_Should_Create_Sequence()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocType.Should().Be("SO");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DOCUMENT_SEQUENCES", FormAction.Create, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateDocumentSequenceCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Sequence_Already_Exists_For_Branch_And_DocType()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.DocumentSequences.Add(SystemModuleTestFixtures.DocumentSequence(branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDocumentSequenceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}
