namespace ZARI.Application.Features.Hr.OvertimeRateRules.Update;

using FluentValidation;

public sealed class UpdateOvertimeRateRuleValidator : AbstractValidator<UpdateOvertimeRateRuleCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];
    private static readonly string[] ValidOtTypes = ["ORDINARY", "REST_DAY", "HOLIDAY"];

    public UpdateOvertimeRateRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OtType)
            .NotEmpty()
            .Must(t => ValidOtTypes.Contains(t))
            .WithMessage($"OT type must be one of: {string.Join(", ", ValidOtTypes)}.");
        RuleFor(x => x.Multiplier).GreaterThan(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
