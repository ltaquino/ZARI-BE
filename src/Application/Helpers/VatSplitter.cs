namespace ZARI.Application.Helpers;

/// <summary>
/// Splits a VAT-inclusive amount into its VAT-exclusive (net) portion and the VAT itself, at the
/// Philippine 12% rate. Exact C# port of the FE's ZARI-FE/src/features/sales/salesInvoices/
/// calculator.ts splitVat() — same math (VAT = amount * rate / (1 + rate)), same 4-decimal
/// rounding — so a Purchase Book/Sales Book report line always agrees with what the FE previews.
/// VATABLE splits the amount VAT-in; ZERO_RATED/VAT_EXEMPT pass the amount through untouched with
/// zero VAT. Shared cross-cutting math (no state, no dependencies) reused by both the Purchase Book
/// report (Purchasing) and the Sales Book report (Sales) — safe for either module to call.
/// </summary>
public static class VatSplitter
{
    public const decimal VatRate = 0.12m;

    public static (decimal NetOfVat, decimal VatAmount) Split(decimal amount, string vatType)
    {
        if (vatType != "VATABLE") return (amount, 0m);

        var vatAmount = Math.Round(amount * VatRate / (1 + VatRate), 4);
        return (amount - vatAmount, vatAmount);
    }
}
