namespace ZARI.Application.Features.Accounting.FiscalYears.Create;

using FluentValidation;

public sealed class CreateFiscalYearValidator : AbstractValidator<CreateFiscalYearCommand>
{
    public CreateFiscalYearValidator()
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
