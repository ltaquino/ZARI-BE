namespace ZARI.Application.Features.Sales.SalesOrders.Cancel;

using FluentValidation;

public sealed class CancelSalesOrderValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
