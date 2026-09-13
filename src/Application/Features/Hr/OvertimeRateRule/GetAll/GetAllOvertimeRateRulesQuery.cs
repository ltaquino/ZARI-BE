namespace ZARI.Application.Features.Hr.OvertimeRateRules.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Domain.Common;

public sealed record GetAllOvertimeRateRulesQuery : IQuery<Result<List<OvertimeRateRuleResponse>>>;
