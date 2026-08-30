namespace ZARI.Application.Features.Sales.SalesReturns.Approve;

using FluentValidation;

public sealed class ApproveSalesReturnValidator : AbstractValidator<ApproveSalesReturnCommand>
{
    public ApproveSalesReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
