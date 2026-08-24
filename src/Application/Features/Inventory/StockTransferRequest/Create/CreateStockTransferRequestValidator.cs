namespace ZARI.Application.Features.Inventory.StockTransferRequests.Create;

using FluentValidation;

public sealed class CreateStockTransferRequestValidator : AbstractValidator<CreateStockTransferRequestCommand>
{
    public CreateStockTransferRequestValidator()
    {
        RuleFor(x => x.SourceBranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestBranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.DestWarehouseId).NotEmpty();
        RuleFor(x => x.RequestDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x).Must(x => x.SourceBranchId != x.DestBranchId)
            .WithMessage("The fulfilling branch must be different from the requesting branch.");

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.QtyRequested).GreaterThan(0);
        });
    }
}
