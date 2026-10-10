namespace ZARI.Application.Features.Inventory.Warehouses.Update;

using FluentValidation;

public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    // "Vehicle" is accepted here only so a truck's own warehouse (created by CreateVehicleCommandHandler)
    // can still be edited — UpdateWarehouseCommandHandler blocks switching any warehouse to/from it.
    private static readonly string[] ValidTypes = ["Main", "Transit", "Damaged", "Consignment", "Vehicle"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
