namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Cancel;

using FluentValidation;

public sealed class CancelGoodsReceiptPoValidator : AbstractValidator<CancelGoodsReceiptPoCommand>
{
    public CancelGoodsReceiptPoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
