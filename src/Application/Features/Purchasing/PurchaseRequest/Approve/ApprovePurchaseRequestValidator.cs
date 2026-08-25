namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Approve;

using FluentValidation;

public sealed class ApprovePurchaseRequestValidator : AbstractValidator<ApprovePurchaseRequestCommand>
{
    public ApprovePurchaseRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
