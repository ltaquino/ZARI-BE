namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Domain.Common;

public sealed record CreateWithholdingTaxBracketCommand(
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal BaseTax,
    decimal RateOverExcess,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand<Result<WithholdingTaxBracketResponse>>;
