namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Cancel;

using FluentValidation;

public sealed class CancelPurchaseRequestValidator : AbstractValidator<CancelPurchaseRequestCommand>
{
    public CancelPurchaseRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
