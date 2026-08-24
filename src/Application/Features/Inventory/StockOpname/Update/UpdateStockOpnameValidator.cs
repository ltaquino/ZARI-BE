namespace ZARI.Application.Features.Inventory.StockOpnames.Update;

using FluentValidation;

public sealed class UpdateStockOpnameValidator : AbstractValidator<UpdateStockOpnameCommand>
{
    public UpdateStockOpnameValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
