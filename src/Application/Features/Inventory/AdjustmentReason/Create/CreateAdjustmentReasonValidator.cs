namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Create;

using FluentValidation;

public sealed class CreateAdjustmentReasonValidator : AbstractValidator<CreateAdjustmentReasonCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateAdjustmentReasonValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.GlAccountId).MaximumLength(25);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
