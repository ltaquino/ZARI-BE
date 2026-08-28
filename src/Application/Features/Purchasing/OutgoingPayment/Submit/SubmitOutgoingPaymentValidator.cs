namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Submit;

using FluentValidation;

public sealed class SubmitOutgoingPaymentValidator : AbstractValidator<SubmitOutgoingPaymentCommand>
{
    public SubmitOutgoingPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
