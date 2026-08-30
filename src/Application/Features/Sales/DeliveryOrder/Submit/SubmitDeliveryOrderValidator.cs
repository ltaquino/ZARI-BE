namespace ZARI.Application.Features.Sales.DeliveryOrders.Submit;

using FluentValidation;

public sealed class SubmitDeliveryOrderValidator : AbstractValidator<SubmitDeliveryOrderCommand>
{
    public SubmitDeliveryOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
