namespace ZARI.Application.Features.Sales.DeliveryOrders.Cancel;

using FluentValidation;

public sealed class CancelDeliveryOrderValidator : AbstractValidator<CancelDeliveryOrderCommand>
{
    public CancelDeliveryOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
