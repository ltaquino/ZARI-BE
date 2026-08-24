namespace ZARI.Application.Features.Inventory.GoodsIssues.Create;

using FluentValidation;

public sealed class CreateGoodsIssueValidator : AbstractValidator<CreateGoodsIssueCommand>
{
    private static readonly string[] ValidReferenceTypes = ["STOCK_TRANSFER", "INTERNAL_USE", "DISPOSAL", "PRODUCTION"];

    public CreateGoodsIssueValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ReferenceType).NotEmpty().Must(t => ValidReferenceTypes.Contains(t))
            .WithMessage($"Reference type must be one of: {string.Join(", ", ValidReferenceTypes)}.");
        RuleFor(x => x.GiDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        // Only a STOCK_TRANSFER issue involves a destination branch/warehouse. The other three
        // reference types permanently consume the stock at the source and need a reason instead.
        RuleFor(x => x.DestBranchId).NotEmpty().When(x => x.ReferenceType == "STOCK_TRANSFER")
            .WithMessage("Destination branch is required for a stock transfer.");
        RuleFor(x => x.DestWarehouseId).NotEmpty().When(x => x.ReferenceType == "STOCK_TRANSFER")
            .WithMessage("Destination warehouse is required for a stock transfer.");
        RuleFor(x => x).Must(x => x.DestBranchId != x.BranchId).When(x => x.ReferenceType == "STOCK_TRANSFER")
            .WithMessage("Destination branch must be different from the source branch.");
        RuleFor(x => x.ReasonCode).NotEmpty().When(x => x.ReferenceType != "STOCK_TRANSFER")
            .WithMessage("A reason is required for this type of goods issue.");

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.QtyIssued).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
