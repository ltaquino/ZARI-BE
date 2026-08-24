namespace ZARI.Application.Features.Inventory.AdjustmentReasons.Update;

using FluentValidation;

public sealed class UpdateAdjustmentReasonValidator : AbstractValidator<UpdateAdjustmentReasonCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateAdjustmentReasonValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.GlAccountId).MaximumLength(25);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
