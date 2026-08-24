namespace ZARI.Application.Features.Inventory.Items.Update;

using FluentValidation;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
{
    private static readonly string[] ValidItemTypes = ["RawMaterial", "FinishedGood", "Service", "Asset", "Consumable"];
    private static readonly string[] ValidCostingMethods = ["Fifo", "Avg"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.BaseUomId).NotEmpty();

        RuleFor(x => x.ItemType)
            .NotEmpty()
            .Must(t => ValidItemTypes.Contains(t))
            .WithMessage($"Item type must be one of: {string.Join(", ", ValidItemTypes)}.");

        RuleFor(x => x.CostingMethod)
            .NotEmpty()
            .Must(m => ValidCostingMethods.Contains(m))
            .WithMessage($"Costing method must be one of: {string.Join(", ", ValidCostingMethods)}.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.SalesAccountId).MaximumLength(25);
        RuleFor(x => x.PurchaseAccountId).MaximumLength(25);
        RuleFor(x => x.InventoryAccountId).MaximumLength(25);
        RuleFor(x => x.CogsAccountId).MaximumLength(25);
    }
}
