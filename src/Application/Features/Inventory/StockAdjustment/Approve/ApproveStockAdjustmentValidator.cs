namespace ZARI.Application.Features.Inventory.StockAdjustments.Approve;

using FluentValidation;

public sealed class ApproveStockAdjustmentValidator : AbstractValidator<ApproveStockAdjustmentCommand>
{
    public ApproveStockAdjustmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
