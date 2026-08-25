namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class ExchangeRate : AuditableEntity
{
    public string CurrencyId { get; set; } = default!;
    public Currency Currency { get; set; } = default!;

    public DateTimeOffset RateDate { get; set; }
    public decimal RateToBase { get; set; }
}
