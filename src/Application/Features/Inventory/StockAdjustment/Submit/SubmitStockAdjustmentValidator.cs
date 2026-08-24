namespace ZARI.Application.Features.Inventory.StockAdjustments.Submit;

using FluentValidation;

public sealed class SubmitStockAdjustmentValidator : AbstractValidator<SubmitStockAdjustmentCommand>
{
    public SubmitStockAdjustmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
