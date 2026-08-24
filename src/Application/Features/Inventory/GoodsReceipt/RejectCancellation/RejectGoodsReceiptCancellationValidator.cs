namespace ZARI.Application.Features.Inventory.GoodsReceipts.RejectCancellation;

using FluentValidation;

public sealed class RejectGoodsReceiptCancellationValidator : AbstractValidator<RejectGoodsReceiptCancellationCommand>
{
    public RejectGoodsReceiptCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
