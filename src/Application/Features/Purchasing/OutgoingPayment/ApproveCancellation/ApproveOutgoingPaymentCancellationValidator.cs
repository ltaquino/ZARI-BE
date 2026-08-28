namespace ZARI.Application.Features.Purchasing.OutgoingPayments.ApproveCancellation;

using FluentValidation;

public sealed class ApproveOutgoingPaymentCancellationValidator : AbstractValidator<ApproveOutgoingPaymentCancellationCommand>
{
    public ApproveOutgoingPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
