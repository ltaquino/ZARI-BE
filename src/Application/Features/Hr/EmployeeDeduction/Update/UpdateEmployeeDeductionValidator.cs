namespace ZARI.Application.Features.Hr.EmployeeDeductions.Update;

using FluentValidation;

public sealed class UpdateEmployeeDeductionValidator : AbstractValidator<UpdateEmployeeDeductionCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateEmployeeDeductionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25)
            .Must(c => !ZARI.Domain.Entities.EmployeeDeduction.IsSystemCode(c))
            .WithMessage($"Code '{ZARI.Domain.Entities.EmployeeDeduction.TripShortageCode}' is reserved for trip shortages charged at a route-sales trip close.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.EffectiveDate).When(x => x.EndDate is not null);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
