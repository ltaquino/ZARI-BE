namespace ZARI.Application.Features.Inventory.StockLocationBalances.Move;

using FluentValidation;

public sealed class MoveBetweenLocationsValidator : AbstractValidator<MoveBetweenLocationsCommand>
{
    public MoveBetweenLocationsValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.FromLocationId).NotEmpty();
        RuleFor(x => x.ToLocationId).NotEmpty();
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.BatchNo).MaximumLength(25);
        RuleFor(x => x)
            .Must(x => x.FromLocationId != x.ToLocationId)
            .WithMessage("From and to locations must be different.");
    }
}
