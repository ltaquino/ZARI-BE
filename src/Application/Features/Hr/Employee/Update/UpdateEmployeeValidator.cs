namespace ZARI.Application.Features.Hr.Employees.Update;

using FluentValidation;

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];
    private static readonly string[] ValidEmploymentStatuses = ["PROBATIONARY", "REGULAR", "RESIGNED", "TERMINATED", "RETIRED"];

    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeNo).NotEmpty().MaximumLength(25);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.EmploymentStatus)
            .NotEmpty()
            .Must(s => ValidEmploymentStatuses.Contains(s))
            .WithMessage($"Employment status must be one of: {string.Join(", ", ValidEmploymentStatuses)}.");
    }
}
