namespace ZARI.Application.Features.Sales.PosTerminals.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The BIR invoice series owned by an own-series PosTerminal (a Route Sales truck handheld — see the
/// PosTerminal doc comment).
///
/// Unlike GetNextDocumentNumberCommandHandler's out-of-band compare-and-swap UPDATE, this increments
/// the *tracked* terminal, so the number is committed by the caller's own SaveChanges together with
/// the invoice that uses it — no number is ever burned by an invoice that then fails to save.
/// PosTerminal.OrNextNumber is an EF concurrency token (PosTerminalConfiguration): if anything else
/// allocated from the same terminal in between, that SaveChanges throws DbUpdateConcurrencyException
/// and nothing is written, so a number can never be handed out twice. In practice a truck series has
/// exactly one writer — its own handheld.
/// </summary>
internal static class PosTerminalSeries
{
    public static string Format(PosTerminal terminal, int number) =>
        $"{terminal.OrPrefix}{number.ToString().PadLeft(terminal.OrPaddingLength, '0')}";

    public static async Task<Result<string>> NextAsync(IAppDbContext dbContext, Guid terminalId, CancellationToken cancellationToken)
    {
        var terminal = await dbContext.PosTerminals.FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken);
        if (terminal is null || !terminal.HasOwnSeries)
            return Result.Failure<string>(Error.Validation("PosTerminal.NoOwnSeries", "This POS terminal has no invoice series of its own."));

        var number = Format(terminal, terminal.OrNextNumber);
        terminal.OrNextNumber += 1;
        return Result.Success(number);
    }

    /// <summary>
    /// Records a number the handheld already issued offline. It must be exactly the series' next
    /// number: the device consumes its series strictly in order (voids included — see
    /// VoidRouteInvoiceCommandHandler), and the outbox syncs oldest-first, so anything else means a
    /// number went missing (a lost sale) and must be resolved before later ones can post.
    /// </summary>
    public static async Task<Result> ClaimAsync(IAppDbContext dbContext, Guid terminalId, string number, CancellationToken cancellationToken)
    {
        var terminal = await dbContext.PosTerminals.FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken);
        if (terminal is null || !terminal.HasOwnSeries)
            return Result.Failure(Error.Validation("PosTerminal.NoOwnSeries", "This POS terminal has no invoice series of its own."));

        var expected = Format(terminal, terminal.OrNextNumber);
        if (!string.Equals(number, expected, StringComparison.Ordinal))
            return Result.Failure(Error.Validation("PosTerminal.OutOfSequence",
                $"Invoice number {number} is out of sequence — this handheld's next number is {expected}. Record or void {expected} first."));

        terminal.OrNextNumber += 1;
        return Result.Success();
    }

    /// <summary>The next number without consuming it — what the handheld starts from at trip start.</summary>
    public static string Peek(PosTerminal terminal) => Format(terminal, terminal.OrNextNumber);
}
