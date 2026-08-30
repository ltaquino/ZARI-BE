namespace ZARI.Application.Features.Sales.SalesReturns.ApproveCancellation;

using FluentValidation;

public sealed class ApproveSalesReturnCancellationValidator : AbstractValidator<ApproveSalesReturnCancellationCommand>
{
    public ApproveSalesReturnCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
