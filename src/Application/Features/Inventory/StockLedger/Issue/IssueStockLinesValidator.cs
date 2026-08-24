namespace ZARI.Application.Features.Inventory.StockLedgers.Issue;

using FluentValidation;

public sealed class IssueStockLinesValidator : AbstractValidator<IssueStockLinesCommand>
{
    public IssueStockLinesValidator()
    {
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.BranchId).NotEmpty().MaximumLength(25);
            line.RuleFor(l => l.WarehouseId).NotEmpty();
            line.RuleFor(l => l.BatchNo).MaximumLength(25);
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.ReferenceTable).NotEmpty().MaximumLength(25);
            line.RuleFor(l => l.ReferenceId).NotEmpty().MaximumLength(25);
        });
    }
}
