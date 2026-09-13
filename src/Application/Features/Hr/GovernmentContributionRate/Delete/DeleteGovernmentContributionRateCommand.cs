namespace ZARI.Application.Features.Hr.GovernmentContributionRates.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteGovernmentContributionRateCommand(Guid Id) : ICommand;
