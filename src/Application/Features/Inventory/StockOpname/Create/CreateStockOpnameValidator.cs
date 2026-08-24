namespace ZARI.Application.Features.Inventory.StockOpnames.Create;

using FluentValidation;

public sealed class CreateStockOpnameValidator : AbstractValidator<CreateStockOpnameCommand>
{
    public CreateStockOpnameValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CountDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.SystemQty).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CountedQty).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
