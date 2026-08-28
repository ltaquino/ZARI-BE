namespace ZARI.Application.Features.Purchasing.Suppliers.Create;

using FluentValidation;

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(25);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.TaxId)
            .MaximumLength(25);

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PaymentTermsDays.HasValue);

        RuleFor(x => x.CurrencyId)
            .MaximumLength(25);

        RuleFor(x => x.Address)
            .MaximumLength(300);

        RuleFor(x => x.ContactPerson)
            .MaximumLength(150);

        RuleFor(x => x.ContactNumber)
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(150);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s is "active" or "inactive")
            .WithMessage("Status must be 'active' or 'inactive'.");
    }
}
