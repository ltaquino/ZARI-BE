namespace ZARI.Application.Features.Purchasing.OutgoingPayments.RejectCancellation;

using FluentValidation;

public sealed class RejectOutgoingPaymentCancellationValidator : AbstractValidator<RejectOutgoingPaymentCancellationCommand>
{
    public RejectOutgoingPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
