namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Update;

using FluentValidation;

public sealed class UpdateEmployeeScheduleAssignmentValidator : AbstractValidator<UpdateEmployeeScheduleAssignmentCommand>
{
    public UpdateEmployeeScheduleAssignmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WorkScheduleId).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .When(x => x.EndDate is not null)
            .WithMessage("End date must be on or after the effective date.");
    }
}
