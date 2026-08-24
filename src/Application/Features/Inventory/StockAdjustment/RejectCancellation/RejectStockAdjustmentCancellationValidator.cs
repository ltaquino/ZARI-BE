namespace ZARI.Application.Features.Inventory.StockAdjustments.RejectCancellation;

using FluentValidation;

public sealed class RejectStockAdjustmentCancellationValidator : AbstractValidator<RejectStockAdjustmentCancellationCommand>
{
    public RejectStockAdjustmentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
