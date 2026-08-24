namespace ZARI.Application.Features.Inventory.GoodsIssues.Submit;

using FluentValidation;

public sealed class SubmitGoodsIssueValidator : AbstractValidator<SubmitGoodsIssueCommand>
{
    public SubmitGoodsIssueValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
