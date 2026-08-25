namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Create;

using FluentValidation;

public sealed class CreatePurchaseReturnReasonValidator : AbstractValidator<CreatePurchaseReturnReasonCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePurchaseReturnReasonValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(300);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
