namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Approve;

using FluentValidation;

public sealed class ApprovePurchaseOrderValidator : AbstractValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
