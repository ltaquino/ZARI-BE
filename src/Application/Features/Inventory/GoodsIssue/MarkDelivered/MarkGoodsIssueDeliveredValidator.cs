namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkDelivered;

using FluentValidation;

public sealed class MarkGoodsIssueDeliveredValidator : AbstractValidator<MarkGoodsIssueDeliveredCommand>
{
    public MarkGoodsIssueDeliveredValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(150);
    }
}
