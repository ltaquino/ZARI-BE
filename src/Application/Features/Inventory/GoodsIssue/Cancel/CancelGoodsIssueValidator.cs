namespace ZARI.Application.Features.Inventory.GoodsIssues.Cancel;

using FluentValidation;

public sealed class CancelGoodsIssueValidator : AbstractValidator<CancelGoodsIssueCommand>
{
    public CancelGoodsIssueValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
