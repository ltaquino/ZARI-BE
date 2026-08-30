namespace ZARI.Application.Features.Sales.CustomerPayments.Submit;

using FluentValidation;

public sealed class SubmitCustomerPaymentValidator : AbstractValidator<SubmitCustomerPaymentCommand>
{
    public SubmitCustomerPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
