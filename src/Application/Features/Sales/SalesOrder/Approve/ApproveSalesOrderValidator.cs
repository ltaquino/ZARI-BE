namespace ZARI.Application.Features.Sales.SalesOrders.Approve;

using FluentValidation;

public sealed class ApproveSalesOrderValidator : AbstractValidator<ApproveSalesOrderCommand>
{
    public ApproveSalesOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
