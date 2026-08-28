namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Reject;

using FluentValidation;

public sealed class RejectOutgoingPaymentValidator : AbstractValidator<RejectOutgoingPaymentCommand>
{
    public RejectOutgoingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
