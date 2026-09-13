namespace ZARI.Application.Features.Hr.GovernmentContributionRates.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Domain.Common;

public sealed record GetAllGovernmentContributionRatesQuery : IQuery<Result<List<GovernmentContributionRateResponse>>>;
