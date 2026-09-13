namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Domain.Common;

public sealed record GetAllPagIbigContributionBracketsQuery : IQuery<Result<List<PagIbigContributionBracketResponse>>>;
