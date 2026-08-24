namespace ZARI.Application.Features.Customers.Update;

using FluentValidation;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    private static readonly string[] ValidTypes = ["individual", "business"];
    private static readonly string[] ValidStatuses = ["lead", "active", "inactive"];

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
