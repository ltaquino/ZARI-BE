namespace ZARI.Application.Features.Sales.CustomerPayments.RejectCancellation;

using FluentValidation;

public sealed class RejectCustomerPaymentCancellationValidator : AbstractValidator<RejectCustomerPaymentCancellationCommand>
{
    public RejectCustomerPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
