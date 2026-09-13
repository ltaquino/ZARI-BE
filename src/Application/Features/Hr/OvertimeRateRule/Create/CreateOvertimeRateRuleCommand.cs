namespace ZARI.Application.Features.Hr.OvertimeRateRules.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Domain.Common;

public sealed record CreateOvertimeRateRuleCommand(
    string OtType,
    decimal Multiplier,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand<Result<OvertimeRateRuleResponse>>;
