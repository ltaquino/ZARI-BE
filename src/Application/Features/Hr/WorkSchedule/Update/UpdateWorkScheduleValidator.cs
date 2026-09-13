namespace ZARI.Application.Features.Hr.WorkSchedules.Update;

using FluentValidation;

public sealed class UpdateWorkScheduleValidator : AbstractValidator<UpdateWorkScheduleCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];
    private static readonly string[] ValidDayCodes = ["MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"];

    public UpdateWorkScheduleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.WorkDays)
            .NotEmpty()
            .Must(days => days.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(d => ValidDayCodes.Contains(d)))
            .WithMessage($"Work days must be a comma-separated list of: {string.Join(", ", ValidDayCodes)}.");
        RuleFor(x => x.GracePeriodMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
