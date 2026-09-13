namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateWithholdingTaxBracketCommand(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal BaseTax,
    decimal RateOverExcess,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand;
