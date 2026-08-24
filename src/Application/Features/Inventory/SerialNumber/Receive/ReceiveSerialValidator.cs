namespace ZARI.Application.Features.Inventory.SerialNumbers.Receive;

using FluentValidation;

public sealed class ReceiveSerialValidator : AbstractValidator<ReceiveSerialCommand>
{
    public ReceiveSerialValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}
