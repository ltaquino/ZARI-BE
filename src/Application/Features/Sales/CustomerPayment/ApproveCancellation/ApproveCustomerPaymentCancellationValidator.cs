namespace ZARI.Application.Features.Sales.CustomerPayments.ApproveCancellation;

using FluentValidation;

public sealed class ApproveCustomerPaymentCancellationValidator : AbstractValidator<ApproveCustomerPaymentCancellationCommand>
{
    public ApproveCustomerPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
