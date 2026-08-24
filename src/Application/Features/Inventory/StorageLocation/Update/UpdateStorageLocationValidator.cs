namespace ZARI.Application.Features.Inventory.StorageLocations.Update;

using FluentValidation;

public sealed class UpdateStorageLocationValidator : AbstractValidator<UpdateStorageLocationCommand>
{
    public UpdateStorageLocationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Zone).MaximumLength(25);
        RuleFor(x => x.Aisle).MaximumLength(25);
        RuleFor(x => x.Rack).MaximumLength(25);
        RuleFor(x => x.BinCode).MaximumLength(25);
    }
}
