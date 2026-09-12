namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

/// <summary>
/// Entity builders for the Accounting feature area not already covered by
/// <see cref="LoanTestFixtures"/> (which already has Branch/GlAccount/CostCenter/PaymentMethod, all
/// generic master data reused here) or <see cref="SystemModuleTestFixtures"/> (Currency). Same
/// InMemory-provider caveats apply as documented there.
/// </summary>
internal static class AccountingTestFixtures
{
    public static BankAccount BankAccount(string branchId, Guid glAccountId, string? currencyId = null, string accountName = "Main Account", string accountNumber = "1234567890") => new()
    {
        BranchId = branchId,
        AccountName = accountName,
        AccountNumber = accountNumber,
        BankName = "Test Bank",
        GlAccountId = glAccountId,
        CurrencyId = currencyId
    };

    public static ExchangeRate ExchangeRate(string currencyId, DateTimeOffset? rateDate = null, decimal rateToBase = 56.5m) => new()
    {
        CurrencyId = currencyId,
        RateDate = rateDate ?? DateTimeOffset.UtcNow,
        RateToBase = rateToBase
    };

    public static FiscalYear FiscalYear(string yearName = "FY2026", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string status = "OPEN")
    {
        var start = startDate ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new FiscalYear
        {
            YearName = yearName,
            StartDate = start,
            EndDate = endDate ?? start.AddYears(1).AddDays(-1),
            Status = status
        };
    }

    public static TaxCode TaxCode(string code = "VAT12", string? name = "12% VAT", decimal rate = 12, string taxType = "Vat", Guid? glAccountId = null) => new()
    {
        Code = code,
        Name = name,
        Rate = rate,
        TaxType = taxType,
        GlAccountId = glAccountId,
        CreatedAt = DateTimeOffset.UtcNow
    };

    /// <summary>A balanced two-line DRAFT entry (debitAccountId/creditAccountId) — the shape every ManualJournalEntry workflow test starts from.</summary>
    public static ManualJournalEntry ManualJournalEntry(string branchId, Guid debitAccountId, Guid creditAccountId, decimal amount = 1000, string status = "DRAFT") => new()
    {
        EntryNo = $"MJE-{Guid.NewGuid():N}",
        BranchId = branchId,
        EntryDate = DateTimeOffset.UtcNow,
        Status = status,
        Remarks = "Test manual journal entry",
        Lines =
        [
            new ManualJournalEntryLine { GlAccountId = debitAccountId, DebitAmount = amount, CreditAmount = 0 },
            new ManualJournalEntryLine { GlAccountId = creditAccountId, DebitAmount = 0, CreditAmount = amount }
        ]
    };
}
