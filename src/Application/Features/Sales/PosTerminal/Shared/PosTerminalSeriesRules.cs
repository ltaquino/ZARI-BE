namespace ZARI.Application.Features.Sales.PosTerminals.Shared;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Common;

/// <summary>Shared Create/Update rules for an own-series terminal's BIR invoice series.</summary>
internal static class PosTerminalSeriesRules
{
    /// <summary>SalesInvoice.BirOrSeriesNumber / ZReading.First|LastOrNumber are 25 chars wide.</summary>
    public const int MaxFormattedLength = 25;

    public static void Apply<T>(AbstractValidator<T> v, Func<T, bool> hasOwnSeries, Func<T, string?> prefix,
        Func<T, int?> nextNumber, Func<T, int?> padding)
    {
        v.RuleFor(x => prefix(x)).NotEmpty().MaximumLength(20).When(x => hasOwnSeries(x))
            .WithMessage("An own invoice series needs a prefix of up to 20 characters.");
        v.RuleFor(x => nextNumber(x)).GreaterThanOrEqualTo(1).When(x => hasOwnSeries(x) && nextNumber(x).HasValue)
            .WithMessage("The next invoice number must be 1 or more.");
        v.RuleFor(x => padding(x)).InclusiveBetween(1, 12).When(x => hasOwnSeries(x) && padding(x).HasValue)
            .WithMessage("Invoice number padding must be between 1 and 12 digits.");
        v.RuleFor(x => x).Must(x => (prefix(x)?.Length ?? 0) + (padding(x) ?? 6) <= MaxFormattedLength)
            .When(x => hasOwnSeries(x))
            .WithMessage($"Prefix plus padded number must fit in {MaxFormattedLength} characters.");
    }

    /// <summary>Two series must never be able to produce the same invoice number
    /// (SalesInvoice.BirOrSeriesNumber is globally unique), so a terminal prefix may not repeat
    /// another terminal's or any branch BIR-OR sequence's prefix.</summary>
    public static async Task<Error?> CheckPrefixUniqueAsync(IAppDbContext dbContext, Guid? excludeTerminalId, string prefix, CancellationToken ct)
    {
        if (await dbContext.PosTerminals.AnyAsync(t => t.Id != excludeTerminalId && t.HasOwnSeries && t.OrPrefix == prefix, ct))
            return Error.Conflict("PosTerminal.DuplicatePrefix", $"Invoice prefix '{prefix}' is already used by another terminal.");
        if (await dbContext.DocumentSequences.AnyAsync(s => s.DocType == "BIR-OR" && s.Prefix == prefix, ct))
            return Error.Conflict("PosTerminal.PrefixClashesWithBranch", $"Invoice prefix '{prefix}' is a branch invoice series prefix.");
        return null;
    }
}
