namespace ZARI.Application.Features.Inventory.StockTransferRequests.Cancel;

using FluentValidation;

public sealed class CancelStockTransferRequestValidator : AbstractValidator<CancelStockTransferRequestCommand>
{
    public CancelStockTransferRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
