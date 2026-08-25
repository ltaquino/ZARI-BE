namespace ZARI.Application.Features.Purchasing.GoodsReturns.ApproveCancellation;

using FluentValidation;

public sealed class ApproveGoodsReturnCancellationValidator : AbstractValidator<ApproveGoodsReturnCancellationCommand>
{
    public ApproveGoodsReturnCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
