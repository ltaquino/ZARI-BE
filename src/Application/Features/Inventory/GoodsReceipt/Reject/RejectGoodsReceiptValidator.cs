namespace ZARI.Application.Features.Inventory.GoodsReceipts.Reject;

using FluentValidation;

public sealed class RejectGoodsReceiptValidator : AbstractValidator<RejectGoodsReceiptCommand>
{
    public RejectGoodsReceiptValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
