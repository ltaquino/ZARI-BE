namespace ZARI.Application.Features.Sales.CustomerPayments.Reject;

using FluentValidation;

public sealed class RejectCustomerPaymentValidator : AbstractValidator<RejectCustomerPaymentCommand>
{
    public RejectCustomerPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
