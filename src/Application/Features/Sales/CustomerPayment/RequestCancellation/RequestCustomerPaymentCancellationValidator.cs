namespace ZARI.Application.Features.Sales.CustomerPayments.RequestCancellation;

using FluentValidation;

public sealed class RequestCustomerPaymentCancellationValidator : AbstractValidator<RequestCustomerPaymentCancellationCommand>
{
    public RequestCustomerPaymentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
