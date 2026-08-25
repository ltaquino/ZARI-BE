namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Submit;

using FluentValidation;

public sealed class SubmitPurchaseOrderValidator : AbstractValidator<SubmitPurchaseOrderCommand>
{
    public SubmitPurchaseOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
