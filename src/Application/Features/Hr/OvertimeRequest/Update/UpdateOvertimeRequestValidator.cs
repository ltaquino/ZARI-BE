namespace ZARI.Application.Features.Hr.OvertimeRequests.Update;

using FluentValidation;

public sealed class UpdateOvertimeRequestValidator : AbstractValidator<UpdateOvertimeRequestCommand>
{
    private static readonly string[] ValidOtTypes = ["ORDINARY", "REST_DAY", "HOLIDAY"];

    public UpdateOvertimeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PlannedTimeTo).GreaterThan(x => x.PlannedTimeFrom);
        RuleFor(x => x.OtType)
            .NotEmpty()
            .Must(t => ValidOtTypes.Contains(t))
            .WithMessage($"OT type must be one of: {string.Join(", ", ValidOtTypes)}.");
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}
