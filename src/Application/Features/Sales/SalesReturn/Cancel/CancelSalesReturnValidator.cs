namespace ZARI.Application.Features.Sales.SalesReturns.Cancel;

using FluentValidation;

public sealed class CancelSalesReturnValidator : AbstractValidator<CancelSalesReturnCommand>
{
    public CancelSalesReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
