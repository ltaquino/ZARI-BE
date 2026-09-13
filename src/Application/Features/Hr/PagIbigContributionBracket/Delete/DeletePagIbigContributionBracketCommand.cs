namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePagIbigContributionBracketCommand(Guid Id) : ICommand;
