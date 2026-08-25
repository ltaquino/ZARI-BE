namespace ZARI.Application.Features.Purchasing.GoodsReturns.Cancel;

using FluentValidation;

public sealed class CancelGoodsReturnValidator : AbstractValidator<CancelGoodsReturnCommand>
{
    public CancelGoodsReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
