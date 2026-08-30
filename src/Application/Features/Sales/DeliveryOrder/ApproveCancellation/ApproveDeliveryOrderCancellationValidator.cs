namespace ZARI.Application.Features.Sales.DeliveryOrders.ApproveCancellation;

using FluentValidation;

public sealed class ApproveDeliveryOrderCancellationValidator : AbstractValidator<ApproveDeliveryOrderCancellationCommand>
{
    public ApproveDeliveryOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
