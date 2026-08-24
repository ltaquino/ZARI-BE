namespace ZARI.Application.Features.Accounting.CostCenters.Create;

using FluentValidation;

public sealed class CreateCostCenterValidator : AbstractValidator<CreateCostCenterCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateCostCenterValidator()
    {
        RuleFor(x => x.BranchId).MaximumLength(25);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
