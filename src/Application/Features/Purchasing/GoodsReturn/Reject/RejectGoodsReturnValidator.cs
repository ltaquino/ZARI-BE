namespace ZARI.Application.Features.Purchasing.GoodsReturns.Reject;

using FluentValidation;

public sealed class RejectGoodsReturnValidator : AbstractValidator<RejectGoodsReturnCommand>
{
    public RejectGoodsReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
