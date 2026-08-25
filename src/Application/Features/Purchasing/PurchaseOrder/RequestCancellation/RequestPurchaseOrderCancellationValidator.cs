namespace ZARI.Application.Features.Purchasing.PurchaseOrders.RequestCancellation;

using FluentValidation;

public sealed class RequestPurchaseOrderCancellationValidator : AbstractValidator<RequestPurchaseOrderCancellationCommand>
{
    public RequestPurchaseOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
