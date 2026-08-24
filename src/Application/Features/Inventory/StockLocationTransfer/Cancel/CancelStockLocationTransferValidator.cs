namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Cancel;

using FluentValidation;

public sealed class CancelStockLocationTransferValidator : AbstractValidator<CancelStockLocationTransferCommand>
{
    public CancelStockLocationTransferValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
