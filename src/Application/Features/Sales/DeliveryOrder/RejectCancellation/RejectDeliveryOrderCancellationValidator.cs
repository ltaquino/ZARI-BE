namespace ZARI.Application.Features.Sales.DeliveryOrders.RejectCancellation;

using FluentValidation;

public sealed class RejectDeliveryOrderCancellationValidator : AbstractValidator<RejectDeliveryOrderCancellationCommand>
{
    public RejectDeliveryOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
