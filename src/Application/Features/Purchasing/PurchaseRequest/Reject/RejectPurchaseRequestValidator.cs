namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Reject;

using FluentValidation;

public sealed class RejectPurchaseRequestValidator : AbstractValidator<RejectPurchaseRequestCommand>
{
    public RejectPurchaseRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
