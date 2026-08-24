namespace ZARI.Application.Features.Inventory.StockLedgers.Receive;

using FluentValidation;

public sealed class ReceiveStockValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.BatchNo).MaximumLength(25);
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReferenceTable).NotEmpty().MaximumLength(25);
        RuleFor(x => x.ReferenceId).NotEmpty().MaximumLength(25);
    }
}
