namespace ZARI.Application.Features.Accounting.FiscalYears.Update;

using FluentValidation;

public sealed class UpdateFiscalYearValidator : AbstractValidator<UpdateFiscalYearCommand>
{
    public UpdateFiscalYearValidator()
    {
        RuleFor(x => x.YearName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s is "OPEN" or "CLOSED")
            .WithMessage("Status must be 'OPEN' or 'CLOSED'.");
    }
}
