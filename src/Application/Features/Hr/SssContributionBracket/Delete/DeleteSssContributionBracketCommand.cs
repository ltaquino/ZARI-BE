namespace ZARI.Application.Features.Hr.SssContributionBrackets.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteSssContributionBracketCommand(Guid Id) : ICommand;
