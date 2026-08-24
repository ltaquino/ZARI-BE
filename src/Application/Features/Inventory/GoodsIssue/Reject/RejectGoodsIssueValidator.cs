namespace ZARI.Application.Features.Inventory.GoodsIssues.Reject;

using FluentValidation;

public sealed class RejectGoodsIssueValidator : AbstractValidator<RejectGoodsIssueCommand>
{
    public RejectGoodsIssueValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
