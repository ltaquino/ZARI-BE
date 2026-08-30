namespace ZARI.Application.Features.Sales.SalesReturns.Reject;

using FluentValidation;

public sealed class RejectSalesReturnValidator : AbstractValidator<RejectSalesReturnCommand>
{
    public RejectSalesReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
