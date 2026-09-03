namespace ZARI.Application.Features.Sales.PosTerminals.Update;

using FluentValidation;

public sealed class UpdatePosTerminalValidator : AbstractValidator<UpdatePosTerminalCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdatePosTerminalValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
