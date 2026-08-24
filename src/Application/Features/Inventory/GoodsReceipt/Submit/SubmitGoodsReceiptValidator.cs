namespace ZARI.Application.Features.Inventory.GoodsReceipts.Submit;

using FluentValidation;

public sealed class SubmitGoodsReceiptValidator : AbstractValidator<SubmitGoodsReceiptCommand>
{
    public SubmitGoodsReceiptValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
