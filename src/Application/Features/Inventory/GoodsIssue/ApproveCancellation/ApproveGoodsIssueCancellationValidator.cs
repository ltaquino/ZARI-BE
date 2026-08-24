namespace ZARI.Application.Features.Inventory.GoodsIssues.ApproveCancellation;

using FluentValidation;

public sealed class ApproveGoodsIssueCancellationValidator : AbstractValidator<ApproveGoodsIssueCancellationCommand>
{
    public ApproveGoodsIssueCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
