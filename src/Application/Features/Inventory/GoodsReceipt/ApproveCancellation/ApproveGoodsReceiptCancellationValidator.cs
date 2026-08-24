namespace ZARI.Application.Features.Inventory.GoodsReceipts.ApproveCancellation;

using FluentValidation;

public sealed class ApproveGoodsReceiptCancellationValidator : AbstractValidator<ApproveGoodsReceiptCancellationCommand>
{
    public ApproveGoodsReceiptCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
