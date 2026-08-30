namespace ZARI.Application.Features.Sales.SalesReturns.RequestCancellation;

using FluentValidation;

public sealed class RequestSalesReturnCancellationValidator : AbstractValidator<RequestSalesReturnCancellationCommand>
{
    public RequestSalesReturnCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
