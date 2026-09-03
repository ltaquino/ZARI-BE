namespace ZARI.Application.Features.Sales.PaymentMethods.Update;

using FluentValidation;

public sealed class UpdatePaymentMethodValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdatePaymentMethodValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GlAccountId).NotEmpty();
        RuleFor(x => x.ReferenceNoLabel).MaximumLength(150);
        RuleFor(x => x.ReferenceNoLabel).NotEmpty().When(x => x.RequiresReferenceNo)
            .WithMessage("A reference-number label is required when this method requires a reference number.");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
