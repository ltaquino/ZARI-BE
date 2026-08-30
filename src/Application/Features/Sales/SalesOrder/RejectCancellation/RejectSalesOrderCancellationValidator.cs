namespace ZARI.Application.Features.Sales.SalesOrders.RejectCancellation;

using FluentValidation;

public sealed class RejectSalesOrderCancellationValidator : AbstractValidator<RejectSalesOrderCancellationCommand>
{
    public RejectSalesOrderCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
