namespace ZARI.Application.Features.Purchasing.PurchaseOrders.RejectCancellation;

using FluentValidation;

public sealed class RejectPurchaseOrderCancellationValidator : AbstractValidator<RejectPurchaseOrderCancellationCommand>
{
    public RejectPurchaseOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
