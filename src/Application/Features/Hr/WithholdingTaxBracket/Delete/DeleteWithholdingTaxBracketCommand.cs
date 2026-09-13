namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteWithholdingTaxBracketCommand(Guid Id) : ICommand;
