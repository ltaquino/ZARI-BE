namespace ZARI.Application.Features.Purchasing.GoodsReturns.Submit;

using FluentValidation;

public sealed class SubmitGoodsReturnValidator : AbstractValidator<SubmitGoodsReturnCommand>
{
    public SubmitGoodsReturnValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
