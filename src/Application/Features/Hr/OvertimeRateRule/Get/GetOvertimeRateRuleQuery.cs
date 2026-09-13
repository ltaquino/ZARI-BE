namespace ZARI.Application.Features.Hr.OvertimeRateRules.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetOvertimeRateRuleQuery(Guid Id) : IQuery<Result<OvertimeRateRuleResponse>>;

public sealed record OvertimeRateRuleResponse(
    Guid Id,
    string OtType,
    decimal Multiplier,
    DateTimeOffset EffectiveDate,
    string Status,
    DateTimeOffset CreatedAt);
