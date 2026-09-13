namespace ZARI.Application.Features.Hr.LeaveTypes.Update;

using FluentValidation;

public sealed class UpdateLeaveTypeValidator : AbstractValidator<UpdateLeaveTypeCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];
    private static readonly string[] ValidFrequencies = ["MONTHLY", "ANNUAL"];

    public UpdateLeaveTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccrualFrequency)
            .NotEmpty()
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage($"Accrual frequency must be one of: {string.Join(", ", ValidFrequencies)}.");
        RuleFor(x => x.AccrualAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxCarryOverDays).GreaterThanOrEqualTo(0).When(x => x.MaxCarryOverDays is not null);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
