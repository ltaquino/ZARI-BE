namespace ZARI.Application.Features.Inventory.Warehouses.Create;

using FluentValidation;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    private static readonly string[] ValidTypes = ["Main", "Transit", "Damaged", "Consignment"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateWarehouseValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.WarehouseType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage($"Warehouse type must be one of: {string.Join(", ", ValidTypes)}.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
