namespace ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Create;

using FluentValidation;

public sealed class CreateEmployeeScheduleAssignmentValidator : AbstractValidator<CreateEmployeeScheduleAssignmentCommand>
{
    public CreateEmployeeScheduleAssignmentValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkScheduleId).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .When(x => x.EndDate is not null)
            .WithMessage("End date must be on or after the effective date.");
    }
}
