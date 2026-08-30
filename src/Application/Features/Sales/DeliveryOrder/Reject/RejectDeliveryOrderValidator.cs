namespace ZARI.Application.Features.Sales.DeliveryOrders.Reject;

using FluentValidation;

public sealed class RejectDeliveryOrderValidator : AbstractValidator<RejectDeliveryOrderCommand>
{
    public RejectDeliveryOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
