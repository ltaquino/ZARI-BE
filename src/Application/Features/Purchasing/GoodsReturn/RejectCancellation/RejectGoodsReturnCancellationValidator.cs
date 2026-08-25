namespace ZARI.Application.Features.Purchasing.GoodsReturns.RejectCancellation;

using FluentValidation;

public sealed class RejectGoodsReturnCancellationValidator : AbstractValidator<RejectGoodsReturnCancellationCommand>
{
    public RejectGoodsReturnCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
