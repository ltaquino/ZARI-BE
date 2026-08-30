namespace ZARI.Application.Features.Sales.CustomerPayments.Cancel;

using FluentValidation;

public sealed class CancelCustomerPaymentValidator : AbstractValidator<CancelCustomerPaymentCommand>
{
    public CancelCustomerPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
