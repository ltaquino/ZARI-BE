namespace ZARI.Application.Features.Inventory.StockAdjustments.Create;

using FluentValidation;

public sealed class CreateStockAdjustmentValidator : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.AdjustmentDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");
        RuleFor(x => x.Lines)
            .Must(lines => lines.Any(l => Math.Abs(l.QtyAfter - l.QtyBefore) > 0.0001m))
            .When(x => x.Lines.Count > 0)
            .WithMessage("At least one line must have a variance — remove lines with no change.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.QtyBefore).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.QtyAfter).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
