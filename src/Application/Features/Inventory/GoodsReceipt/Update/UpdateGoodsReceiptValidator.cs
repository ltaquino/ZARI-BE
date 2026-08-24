namespace ZARI.Application.Features.Inventory.GoodsReceipts.Update;

using FluentValidation;

public sealed class UpdateGoodsReceiptValidator : AbstractValidator<UpdateGoodsReceiptCommand>
{
    private static readonly string[] ValidReceiptTypes = ["TRANSFER_IN", "MANUAL"];

    public UpdateGoodsReceiptValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ReceiptType).NotEmpty().Must(t => ValidReceiptTypes.Contains(t))
            .WithMessage($"Receipt type must be one of: {string.Join(", ", ValidReceiptTypes)}.");
        RuleFor(x => x.ReceivedBy).MaximumLength(150);
        RuleFor(x => x.GrDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.ReasonCode).NotEmpty().When(x => x.ReceiptType == "MANUAL")
            .WithMessage("A reason is required for a manual goods receipt.");

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.QtyReceived).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
