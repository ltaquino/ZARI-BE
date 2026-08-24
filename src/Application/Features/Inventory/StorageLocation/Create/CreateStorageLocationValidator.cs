namespace ZARI.Application.Features.Inventory.StorageLocations.Create;

using FluentValidation;

public sealed class CreateStorageLocationValidator : AbstractValidator<CreateStorageLocationCommand>
{
    public CreateStorageLocationValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Zone).MaximumLength(25);
        RuleFor(x => x.Aisle).MaximumLength(25);
        RuleFor(x => x.Rack).MaximumLength(25);
        RuleFor(x => x.BinCode).MaximumLength(25);
    }
}
