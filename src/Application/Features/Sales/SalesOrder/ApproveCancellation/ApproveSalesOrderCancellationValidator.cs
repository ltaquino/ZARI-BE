namespace ZARI.Application.Features.Sales.SalesOrders.ApproveCancellation;

using FluentValidation;

public sealed class ApproveSalesOrderCancellationValidator : AbstractValidator<ApproveSalesOrderCancellationCommand>
{
    public ApproveSalesOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
