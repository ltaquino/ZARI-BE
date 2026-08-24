namespace ZARI.Application.Features.Inventory.StockAdjustments.RequestCancellation;

using FluentValidation;

public sealed class RequestStockAdjustmentCancellationValidator : AbstractValidator<RequestStockAdjustmentCancellationCommand>
{
    public RequestStockAdjustmentCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
