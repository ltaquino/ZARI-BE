namespace ZARI.Application.Features.Inventory.StockLedgers.Reverse;

using FluentValidation;

public sealed class ReverseStockMovementsValidator : AbstractValidator<ReverseStockMovementsCommand>
{
    public ReverseStockMovementsValidator()
    {
        RuleFor(x => x.ReferenceTable).NotEmpty().MaximumLength(25);
        RuleFor(x => x.ReferenceIds).NotEmpty();
    }
}
