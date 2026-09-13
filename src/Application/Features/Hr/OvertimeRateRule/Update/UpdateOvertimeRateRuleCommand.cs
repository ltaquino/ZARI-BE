namespace ZARI.Application.Features.Hr.OvertimeRateRules.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateOvertimeRateRuleCommand(
    Guid Id,
    string OtType,
    decimal Multiplier,
    DateTimeOffset EffectiveDate,
    string Status) : ICommand;
