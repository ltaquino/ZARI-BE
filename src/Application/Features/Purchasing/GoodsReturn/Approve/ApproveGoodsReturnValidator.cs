namespace ZARI.Application.Features.Purchasing.GoodsReturns.Approve;

using FluentValidation;

public sealed class ApproveGoodsReturnValidator : AbstractValidator<ApproveGoodsReturnCommand>
{
    public ApproveGoodsReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
