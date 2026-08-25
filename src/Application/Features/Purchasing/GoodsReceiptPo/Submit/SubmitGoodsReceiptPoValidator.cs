namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.Submit;

using FluentValidation;

public sealed class SubmitGoodsReceiptPoValidator : AbstractValidator<SubmitGoodsReceiptPoCommand>
{
    public SubmitGoodsReceiptPoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
