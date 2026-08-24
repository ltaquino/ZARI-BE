namespace ZARI.Application.Features.Inventory.StockLocationBalances.Receive;

using FluentValidation;

public sealed class ReceiveIntoLocationValidator : AbstractValidator<ReceiveIntoLocationCommand>
{
    public ReceiveIntoLocationValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.BatchNo).MaximumLength(25);
    }
}
