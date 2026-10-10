namespace ZARI.Application.Features.RouteSales.Vehicles.Update;

using FluentValidation;
using ZARI.Application.Features.Sales.PosTerminals.Shared;

public sealed class UpdateVehicleValidator : AbstractValidator<UpdateVehicleCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateVehicleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PlateNo).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(150);
        RuleFor(x => x.Status).Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        PosTerminalSeriesRules.Apply<UpdateVehicleCommand>(this, _ => true, x => x.OrPrefix, x => x.OrNextNumber, x => x.OrPaddingLength);
    }
}
