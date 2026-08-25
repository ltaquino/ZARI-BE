namespace ZARI.Application.Features.Purchasing.PurchaseOrders.ApproveCancellation;

using FluentValidation;

public sealed class ApprovePurchaseOrderCancellationValidator : AbstractValidator<ApprovePurchaseOrderCancellationCommand>
{
    public ApprovePurchaseOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
