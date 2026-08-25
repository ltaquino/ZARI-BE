namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RejectCancellation;

using FluentValidation;

public sealed class RejectGoodsReceiptPoCancellationValidator : AbstractValidator<RejectGoodsReceiptPoCancellationCommand>
{
    public RejectGoodsReceiptPoCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
