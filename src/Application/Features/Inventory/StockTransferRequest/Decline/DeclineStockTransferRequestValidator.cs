namespace ZARI.Application.Features.Inventory.StockTransferRequests.Decline;

using FluentValidation;

public sealed class DeclineStockTransferRequestValidator : AbstractValidator<DeclineStockTransferRequestCommand>
{
    public DeclineStockTransferRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DeclinedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
