namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Approve;

using FluentValidation;

public sealed class ApproveGoodsReceiptPoValidator : AbstractValidator<ApproveGoodsReceiptPoCommand>
{
    public ApproveGoodsReceiptPoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
