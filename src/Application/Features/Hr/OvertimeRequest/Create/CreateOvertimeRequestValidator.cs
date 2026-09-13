namespace ZARI.Application.Features.Hr.OvertimeRequests.Create;

using FluentValidation;

public sealed class CreateOvertimeRequestValidator : AbstractValidator<CreateOvertimeRequestCommand>
{
    private static readonly string[] ValidOtTypes = ["ORDINARY", "REST_DAY", "HOLIDAY"];

    public CreateOvertimeRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PlannedTimeTo).GreaterThan(x => x.PlannedTimeFrom);
        RuleFor(x => x.OtType)
            .NotEmpty()
            .Must(t => ValidOtTypes.Contains(t))
            .WithMessage($"OT type must be one of: {string.Join(", ", ValidOtTypes)}.");
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}
