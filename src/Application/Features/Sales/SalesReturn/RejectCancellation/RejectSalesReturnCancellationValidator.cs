namespace ZARI.Application.Features.Sales.SalesReturns.RejectCancellation;

using FluentValidation;

public sealed class RejectSalesReturnCancellationValidator : AbstractValidator<RejectSalesReturnCancellationCommand>
{
    public RejectSalesReturnCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
