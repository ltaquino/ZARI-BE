namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Create;

using FluentValidation;

public sealed class CreateStockLocationTransferValidator : AbstractValidator<CreateStockLocationTransferCommand>
{
    public CreateStockLocationTransferValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.TransferDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.FromLocationId).NotEmpty();
            line.RuleFor(l => l.ToLocationId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l).Must(l => l.FromLocationId != l.ToLocationId)
                .WithMessage("From and to locations must be different.");
        });
    }
}
