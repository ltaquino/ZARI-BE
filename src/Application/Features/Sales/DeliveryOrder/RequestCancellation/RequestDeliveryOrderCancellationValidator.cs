namespace ZARI.Application.Features.Sales.DeliveryOrders.RequestCancellation;

using FluentValidation;

public sealed class RequestDeliveryOrderCancellationValidator : AbstractValidator<RequestDeliveryOrderCancellationCommand>
{
    public RequestDeliveryOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
