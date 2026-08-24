namespace ZARI.Application.Features.Inventory.GoodsReceipts.RequestCancellation;

using FluentValidation;

public sealed class RequestGoodsReceiptCancellationValidator : AbstractValidator<RequestGoodsReceiptCancellationCommand>
{
    public RequestGoodsReceiptCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
