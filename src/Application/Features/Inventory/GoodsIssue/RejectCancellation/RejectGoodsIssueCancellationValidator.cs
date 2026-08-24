namespace ZARI.Application.Features.Inventory.GoodsIssues.RejectCancellation;

using FluentValidation;

public sealed class RejectGoodsIssueCancellationValidator : AbstractValidator<RejectGoodsIssueCancellationCommand>
{
    public RejectGoodsIssueCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
