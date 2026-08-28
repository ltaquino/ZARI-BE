namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Approve;

using FluentValidation;

public sealed class ApproveOutgoingPaymentValidator : AbstractValidator<ApproveOutgoingPaymentCommand>
{
    public ApproveOutgoingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
