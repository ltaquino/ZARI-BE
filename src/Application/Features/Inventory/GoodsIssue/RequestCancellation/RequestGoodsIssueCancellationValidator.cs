namespace ZARI.Application.Features.Inventory.GoodsIssues.RequestCancellation;

using FluentValidation;

public sealed class RequestGoodsIssueCancellationValidator : AbstractValidator<RequestGoodsIssueCancellationCommand>
{
    public RequestGoodsIssueCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
