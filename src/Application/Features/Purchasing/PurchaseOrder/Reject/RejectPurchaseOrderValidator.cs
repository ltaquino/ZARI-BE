namespace ZARI.Application.Features.Purchasing.PurchaseOrders.Reject;

using FluentValidation;

public sealed class RejectPurchaseOrderValidator : AbstractValidator<RejectPurchaseOrderCommand>
{
    public RejectPurchaseOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
