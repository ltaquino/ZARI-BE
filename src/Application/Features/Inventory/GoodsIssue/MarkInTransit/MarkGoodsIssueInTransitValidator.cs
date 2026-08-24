namespace ZARI.Application.Features.Inventory.GoodsIssues.MarkInTransit;

using FluentValidation;

public sealed class MarkGoodsIssueInTransitValidator : AbstractValidator<MarkGoodsIssueInTransitCommand>
{
    public MarkGoodsIssueInTransitValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(150);
    }
}
