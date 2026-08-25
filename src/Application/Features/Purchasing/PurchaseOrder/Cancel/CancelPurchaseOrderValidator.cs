namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Cancel;

using FluentValidation;

public sealed class CancelPurchaseOrderValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
