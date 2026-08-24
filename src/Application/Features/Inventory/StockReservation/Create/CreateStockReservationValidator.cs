namespace ZARI.Application.Features.Inventory.StockReservations.Create;

using FluentValidation;

public sealed class CreateStockReservationValidator : AbstractValidator<CreateStockReservationCommand>
{
    public CreateStockReservationValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.QtyReserved).GreaterThan(0).WithMessage("Reserved quantity must be greater than zero.");
        RuleFor(x => x.ReservedDate).NotEmpty();
        RuleFor(x => x.ReferenceNote).MaximumLength(300);
    }
}
