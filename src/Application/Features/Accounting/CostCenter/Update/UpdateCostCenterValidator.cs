namespace ZARI.Application.Features.Accounting.CostCenters.Update;

using FluentValidation;

public sealed class UpdateCostCenterValidator : AbstractValidator<UpdateCostCenterCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateCostCenterValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).MaximumLength(25);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
