namespace ZARI.Application.Features.Sales.PosClosing.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>Validates the optional terminal a closing is scoped to: it must belong to the branch
/// and own its BIR series — a shared-series store till is always closed as part of the branch.</summary>
internal static class ClosingScope
{
    public static async Task<Result<PosTerminal?>> ResolveAsync(
        IAppDbContext dbContext, string branchId, Guid? posTerminalId, bool track, CancellationToken cancellationToken)
    {
        if (posTerminalId is null)
            return Result.Success<PosTerminal?>(null);

        var query = track ? dbContext.PosTerminals : dbContext.PosTerminals.AsNoTracking();
        var terminal = await query.FirstOrDefaultAsync(t => t.Id == posTerminalId && t.BranchId == branchId, cancellationToken);
        if (terminal is null)
            return Result.Failure<PosTerminal?>(Error.NotFound("PosClosing.TerminalNotFound", "The POS terminal was not found at this branch."));
        if (!terminal.HasOwnSeries)
            return Result.Failure<PosTerminal?>(Error.Validation("PosClosing.TerminalSharesBranchSeries",
                "This terminal shares the branch invoice series — run the branch closing instead."));
        return Result.Success<PosTerminal?>(terminal);
    }
}
