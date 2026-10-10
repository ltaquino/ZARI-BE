namespace ZARI.Application.Features.Sales.PosTerminals.Create;

using FluentValidation;
using ZARI.Application.Features.Sales.PosTerminals.Shared;

public sealed class CreatePosTerminalValidator : AbstractValidator<CreatePosTerminalCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePosTerminalValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        PosTerminalSeriesRules.Apply<CreatePosTerminalCommand>(this, x => x.HasOwnSeries, x => x.OrPrefix, x => x.OrNextNumber, x => x.OrPaddingLength);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
