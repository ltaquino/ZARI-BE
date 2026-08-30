namespace ZARI.Application.Features.Sales.SalesReturns.Submit;

using FluentValidation;

public sealed class SubmitSalesReturnValidator : AbstractValidator<SubmitSalesReturnCommand>
{
    public SubmitSalesReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
