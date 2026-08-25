namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Submit;

using FluentValidation;

public sealed class SubmitPurchaseRequestValidator : AbstractValidator<SubmitPurchaseRequestCommand>
{
    public SubmitPurchaseRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
