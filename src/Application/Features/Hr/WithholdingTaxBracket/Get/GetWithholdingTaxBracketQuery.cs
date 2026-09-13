namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetWithholdingTaxBracketQuery(Guid Id) : IQuery<Result<WithholdingTaxBracketResponse>>;

public sealed record WithholdingTaxBracketResponse(
    Guid Id,
    decimal SalaryFrom,
    decimal SalaryTo,
    decimal BaseTax,
    decimal RateOverExcess,
    DateTimeOffset EffectiveDate,
    string Status,
    DateTimeOffset CreatedAt);
