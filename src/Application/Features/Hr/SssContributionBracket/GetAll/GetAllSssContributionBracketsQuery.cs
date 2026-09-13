namespace ZARI.Application.Features.Hr.SssContributionBrackets.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed record GetAllSssContributionBracketsQuery : IQuery<Result<List<SssContributionBracketResponse>>>;
