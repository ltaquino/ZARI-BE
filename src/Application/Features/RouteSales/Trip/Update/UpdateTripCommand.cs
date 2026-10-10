namespace ZARI.Application.Features.RouteSales.Trips.Update;

using FluentValidation;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Domain.Common;

/// <summary>Re-plans a DRAFT trip; orders and extra lines are replaced wholesale.</summary>
public sealed record UpdateTripCommand(
    Guid Id,
    Guid VehicleId,
    Guid? RouteId,
    Guid SourceWarehouseId,
    string DriverUserId,
    Guid? DriverEmployeeId,
    string? HelperNames,
    DateTimeOffset TripDate,
    string? Remarks,
    List<Guid> SalesOrderIds,
    List<TripExtraLineInput> ExtraLines) : ICommand<Result<TripResponse>>, ITripPlan;

public sealed class UpdateTripValidator : AbstractValidator<UpdateTripCommand>
{
    public UpdateTripValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        TripPlanning.ApplyRules(this);
    }
}
