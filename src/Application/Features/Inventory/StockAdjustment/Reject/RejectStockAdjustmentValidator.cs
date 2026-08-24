namespace ZARI.Application.Features.Inventory.StockAdjustments.Reject;

using FluentValidation;

public sealed class RejectStockAdjustmentValidator : AbstractValidator<RejectStockAdjustmentCommand>
{
    public RejectStockAdjustmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
