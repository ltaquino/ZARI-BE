namespace ZARI.Application.Features.Inventory.GoodsReceipts.Approve;

using FluentValidation;

public sealed class ApproveGoodsReceiptValidator : AbstractValidator<ApproveGoodsReceiptCommand>
{
    public ApproveGoodsReceiptValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
