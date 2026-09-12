namespace ZARI.Application.UnitTests.Features.SystemModule.DocumentSequence;

using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.UnitTests.TestSupport;

/// <summary>
/// The success path (a configured sequence exists) uses a compare-and-swap `ExecuteUpdateAsync`
/// call to atomically claim the next number — EF Core's InMemory provider does not support
/// ExecuteUpdate/ExecuteUpdateAsync at all (throws InvalidOperationException), so only the
/// no-sequence-configured fallback path is covered here (same gap as every other ExecuteUpdateAsync
/// handler documented on LoanTestFixtures/SystemModuleTestFixtures).
/// </summary>
public sealed class GetNextDocumentNumberCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Timestamp_Fallback_When_No_Sequence_Configured()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetNextDocumentNumberCommandHandler(db);

        var result = await handler.HandleAsync(new GetNextDocumentNumberCommand("br-1", "SO"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocumentNumber.Should().StartWith("SO-");
    }
}
