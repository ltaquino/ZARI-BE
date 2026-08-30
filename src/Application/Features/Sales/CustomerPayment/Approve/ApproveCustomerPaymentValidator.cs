namespace ZARI.Application.Features.Sales.CustomerPayments.Approve;

using FluentValidation;

public sealed class ApproveCustomerPaymentValidator : AbstractValidator<ApproveCustomerPaymentCommand>
{
    public ApproveCustomerPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
