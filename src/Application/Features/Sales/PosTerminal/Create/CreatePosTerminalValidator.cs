namespace ZARI.Application.Features.Sales.PosTerminals.Create;

using FluentValidation;

public sealed class CreatePosTerminalValidator : AbstractValidator<CreatePosTerminalCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePosTerminalValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
