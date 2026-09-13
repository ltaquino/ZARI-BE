namespace ZARI.Application.UnitTests.Features.Inventory.StockLedger;

using ZARI.Application.Features.Inventory.StockLedgers.Reverse;
using ZARI.Application.UnitTests.TestSupport;

/// <summary>
/// Same InMemory transaction blocker — only the no-matching-originals guard clause (which returns
/// before `BeginTransactionAsync`) is testable; the actual reversal logic and its "CannotReverse"
/// validations all live inside the transaction delegate.
/// </summary>
public sealed class ReverseStockMovementsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Succeed_When_No_Matching_Ledger_Rows_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReverseStockMovementsCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseStockMovementsCommand("SalesInvoiceLine", ["ref-missing"]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
