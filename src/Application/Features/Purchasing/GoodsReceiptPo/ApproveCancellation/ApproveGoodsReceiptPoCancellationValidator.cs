namespace ZARI.Application.Features.Purchasing.GoodsReceiptPos.ApproveCancellation;

using FluentValidation;

public sealed class ApproveGoodsReceiptPoCancellationValidator : AbstractValidator<ApproveGoodsReceiptPoCancellationCommand>
{
    public ApproveGoodsReceiptPoCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
