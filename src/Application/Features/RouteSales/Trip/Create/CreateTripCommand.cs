namespace ZARI.Application.Features.RouteSales.Trips.Create;

using FluentValidation;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;

/// <summary>Plans a trip (DRAFT): the truck, who drives it, which approved orders it delivers and
/// what extra stock it carries for van sales.</summary>
public sealed record CreateTripCommand(
    string BranchId,
    Guid VehicleId,
    Guid? RouteId,
    Guid SourceWarehouseId,
    string DriverUserId,
    Guid? DriverEmployeeId,
    string? HelperNames,
    DateTimeOffset TripDate,
    string? Remarks,
    List<Guid> SalesOrderIds,
    List<TripExtraLineInput> ExtraLines,
    string? CreatedBy) : ICommand<Result<TripResponse>>, ITripPlan;

public sealed class CreateTripValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        TripPlanning.ApplyRules(this);
    }
}
