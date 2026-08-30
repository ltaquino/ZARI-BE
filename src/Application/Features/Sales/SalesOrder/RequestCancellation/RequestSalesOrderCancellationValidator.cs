namespace ZARI.Application.Features.Sales.SalesOrders.RequestCancellation;

using FluentValidation;

public sealed class RequestSalesOrderCancellationValidator : AbstractValidator<RequestSalesOrderCancellationCommand>
{
    public RequestSalesOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
