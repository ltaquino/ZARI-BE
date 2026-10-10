namespace ZARI.Application.Features.RouteSales.Vehicles.Create;

using FluentValidation;
using ZARI.Application.Features.Sales.PosTerminals.Shared;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateVehicleValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        // 20, not 25: the warehouse code is "TRK-" + Code and Warehouse.Code is 25 wide.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PlateNo).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Description).MaximumLength(150);
        RuleFor(x => x.Status).Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        PosTerminalSeriesRules.Apply<CreateVehicleCommand>(this, _ => true, x => x.OrPrefix, x => x.OrNextNumber, x => x.OrPaddingLength);
    }
}
