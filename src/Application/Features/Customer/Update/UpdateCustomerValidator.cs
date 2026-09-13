namespace ZARI.Application.Features.Customers.Update;

using FluentValidation;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    private static readonly string[] ValidTypes = ["individual", "business"];
    private static readonly string[] ValidStatuses = ["lead", "active", "inactive"];
    private static readonly string[] ValidHouseOwnerOrLesseeValues = ["OWN", "RENT", "LEASE", "OTHER"];

    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(300);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0).When(x => x.PaymentTermsDays.HasValue);
        RuleFor(x => x.StandingDiscountPct).InclusiveBetween(0, 100).When(x => x.StandingDiscountPct.HasValue);

        RuleFor(x => x.Tin).MaximumLength(100);
        RuleFor(x => x.SssOrGsisNo).MaximumLength(100);
        RuleFor(x => x.Sex).MaximumLength(25);
        RuleFor(x => x.CivilStatus).MaximumLength(25);
        RuleFor(x => x.DependentsCount).GreaterThanOrEqualTo(0).When(x => x.DependentsCount.HasValue);
        RuleFor(x => x.Employer).MaximumLength(150);
        RuleFor(x => x.EmployerPosition).MaximumLength(150);
        RuleFor(x => x.NetIncomeLastYear).GreaterThanOrEqualTo(0).When(x => x.NetIncomeLastYear.HasValue);
        RuleFor(x => x.PriorResidenceHistory).MaximumLength(300);
        RuleFor(x => x.PriorEmploymentHistory).MaximumLength(300);
        RuleFor(x => x.HousingStatus).MaximumLength(25);
        RuleFor(x => x.BankAccountInfo).MaximumLength(300);
        RuleFor(x => x.OtherAssetsNotes).MaximumLength(300);

        // CIC CSDF "ID" record fields — see Customer.cs doc comment / LoanCicContext.md §4.1.
        RuleFor(x => x.Title).MaximumLength(25);
        RuleFor(x => x.FirstName).MaximumLength(150);
        RuleFor(x => x.MiddleName).MaximumLength(150);
        RuleFor(x => x.LastName).MaximumLength(150);
        RuleFor(x => x.Suffix).MaximumLength(10);
        RuleFor(x => x.PlaceOfBirth).MaximumLength(150);
        RuleFor(x => x.CountryOfBirthCode).MaximumLength(50);
        RuleFor(x => x.NationalityCode).MaximumLength(50);
        RuleFor(x => x.AddressSubdivision).MaximumLength(150);
        RuleFor(x => x.AddressBarangay).MaximumLength(150);
        RuleFor(x => x.AddressCity).MaximumLength(150);
        RuleFor(x => x.AddressProvince).MaximumLength(150);
        RuleFor(x => x.AddressPostalCode).MaximumLength(6);
        RuleFor(x => x.AddressCountryCode).MaximumLength(50);
        RuleFor(x => x.AddressHouseOwnerOrLessee)
            .Must(v => ValidHouseOwnerOrLesseeValues.Contains(v))
            .When(x => x.AddressHouseOwnerOrLessee is not null)
            .WithMessage($"AddressHouseOwnerOrLessee must be one of: {string.Join(", ", ValidHouseOwnerOrLesseeValues)}.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
