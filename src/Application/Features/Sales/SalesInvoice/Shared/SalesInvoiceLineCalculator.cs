namespace ZARI.Application.Features.Sales.SalesInvoices.Shared;

/// <summary>
/// Pure per-line VAT/discount math — implements the 5-step order from SalesModuleContext.md §3.7:
/// (1) gross = Qty x UnitPrice (VAT-inclusive); (2) apply the discretionary discount to get Net;
/// (3) a statutory-discount line instead strips VAT from gross FIRST to get the VAT-exempt base,
/// then applies the statutory type's fixed % to that base — never combined with a discretionary
/// discount on the same line; (4)/(5) a VATABLE line's Net splits into VatAmount + VAT-exclusive
/// sales, a VAT_EXEMPT/ZERO_RATED line's whole Net is the exempt/zero-rated sales figure.
/// No side effects, no DB access — safe to call from the Approve/quick-post-Create GL posting path
/// and mirrored on the FE for a live totals preview, so nothing derived is duplicated ambiguously.
/// </summary>
internal static class SalesInvoiceLineCalculator
{
    /// 12% Philippine VAT rate — the same figure used throughout for both extracting VAT from a
    /// VAT-inclusive gross and stripping it from a statutory line's base.
    public const decimal VatRate = 0.12m;

    public sealed record LineInput(
        decimal Qty,
        decimal UnitPrice,
        decimal DiscountPct,
        string VatType,
        decimal? StatutoryDiscountPct);

    /// <summary>NetAmount is pre-header-discount — the caller applies the document's own header
    /// discount % uniformly across every line's NetAmount before calling <see cref="SplitVat"/>.</summary>
    public sealed record LineResult(decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, string EffectiveVatType);

    public static LineResult Calculate(LineInput input)
    {
        var gross = Math.Round(input.Qty * input.UnitPrice, 4);

        if (input.StatutoryDiscountPct.HasValue)
        {
            // Statutory: strip VAT from gross FIRST to get the VAT-exempt base, then apply the fixed %.
            var vatExemptBase = Math.Round(gross / (1 + VatRate), 4);
            var statutoryDiscountAmount = Math.Round(vatExemptBase * (input.StatutoryDiscountPct.Value / 100m), 4);
            var netAmount = vatExemptBase - statutoryDiscountAmount;
            return new LineResult(gross, statutoryDiscountAmount, netAmount, "VAT_EXEMPT");
        }

        var discretionaryDiscountAmount = Math.Round(gross * (input.DiscountPct / 100m), 4);
        var netAfterDiscount = gross - discretionaryDiscountAmount;
        return new LineResult(gross, discretionaryDiscountAmount, netAfterDiscount, input.VatType);
    }

    /// <summary>Splits a (post-header-discount) net amount into VAT-exclusive sales + VAT, per its effective VAT type.</summary>
    public static (decimal NetOfVat, decimal VatAmount) SplitVat(decimal netAmount, string effectiveVatType)
    {
        if (effectiveVatType != "VATABLE") return (netAmount, 0);
        var vatAmount = Math.Round(netAmount * VatRate / (1 + VatRate), 4);
        return (netAmount - vatAmount, vatAmount);
    }
}
