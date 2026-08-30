namespace ZARI.Application.Features.Sales.DeliveryOrders.Approve;

using FluentValidation;

public sealed class ApproveDeliveryOrderValidator : AbstractValidator<ApproveDeliveryOrderCommand>
{
    public ApproveDeliveryOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
