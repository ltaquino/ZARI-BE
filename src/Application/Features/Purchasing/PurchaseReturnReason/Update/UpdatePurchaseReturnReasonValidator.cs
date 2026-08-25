namespace ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Update;

using FluentValidation;

public sealed class UpdatePurchaseReturnReasonValidator : AbstractValidator<UpdatePurchaseReturnReasonCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdatePurchaseReturnReasonValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(300);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
