namespace ZARI.Application.Features.Hr.OvertimeRateRules.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteOvertimeRateRuleCommand(Guid Id) : ICommand;
