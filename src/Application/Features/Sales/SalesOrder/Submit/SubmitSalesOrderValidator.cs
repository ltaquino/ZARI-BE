namespace ZARI.Application.Features.Sales.SalesOrders.Submit;

using FluentValidation;

public sealed class SubmitSalesOrderValidator : AbstractValidator<SubmitSalesOrderCommand>
{
    public SubmitSalesOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
