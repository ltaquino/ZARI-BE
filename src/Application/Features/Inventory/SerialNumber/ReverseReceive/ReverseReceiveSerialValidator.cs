namespace ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;

using FluentValidation;

public sealed class ReverseReceiveSerialValidator : AbstractValidator<ReverseReceiveSerialCommand>
{
    public ReverseReceiveSerialValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RevertTo).Must(r => r is "IN_TRANSIT" or "REMOVE")
            .WithMessage("RevertTo must be IN_TRANSIT or REMOVE.");
    }
}
