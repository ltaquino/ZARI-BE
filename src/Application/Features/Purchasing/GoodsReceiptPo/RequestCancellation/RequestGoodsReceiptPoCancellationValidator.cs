namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.RequestCancellation;

using FluentValidation;

public sealed class RequestGoodsReceiptPoCancellationValidator : AbstractValidator<RequestGoodsReceiptPoCancellationCommand>
{
    public RequestGoodsReceiptPoCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
