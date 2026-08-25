namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Reject;

using FluentValidation;

public sealed class RejectGoodsReceiptPoValidator : AbstractValidator<RejectGoodsReceiptPoCommand>
{
    public RejectGoodsReceiptPoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
