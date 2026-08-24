namespace ZARI.Application.Features.Inventory.GoodsIssues.Approve;

using FluentValidation;

public sealed class ApproveGoodsIssueValidator : AbstractValidator<ApproveGoodsIssueCommand>
{
    public ApproveGoodsIssueValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
