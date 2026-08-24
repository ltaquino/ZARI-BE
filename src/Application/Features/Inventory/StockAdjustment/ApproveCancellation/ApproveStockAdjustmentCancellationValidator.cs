namespace ZARI.Application.Features.Inventory.StockAdjustments.ApproveCancellation;

using FluentValidation;

public sealed class ApproveStockAdjustmentCancellationValidator : AbstractValidator<ApproveStockAdjustmentCancellationCommand>
{
    public ApproveStockAdjustmentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
