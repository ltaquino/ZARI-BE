namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>One customer on the route; the list order is the visiting order.</summary>
public sealed record DeliveryRouteStopInput(Guid CustomerId, string? Notes);

internal static class DeliveryRouteShared
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public static void ApplyRules<T>(AbstractValidator<T> v, Func<T, string> code, Func<T, string> name,
        Func<T, string?> description, Func<T, string> status, Func<T, List<DeliveryRouteStopInput>?> stops)
    {
        v.RuleFor(x => code(x)).NotEmpty().MaximumLength(25).WithName("Code");
        v.RuleFor(x => name(x)).NotEmpty().MaximumLength(150).WithName("Name");
        v.RuleFor(x => description(x)).MaximumLength(300).WithName("Description");
        v.RuleFor(x => status(x)).Must(s => ValidStatuses.Contains(s)).WithName("Status")
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        v.RuleFor(x => stops(x)).NotNull().WithName("Stops");
        v.RuleFor(x => stops(x)).Must(s => s == null || s.Select(st => st.CustomerId).Distinct().Count() == s.Count)
            .WithName("Stops").WithMessage("A customer can appear only once on a route.");
        v.RuleForEach(x => stops(x)).ChildRules(stop =>
        {
            stop.RuleFor(s => s.CustomerId).NotEmpty();
            stop.RuleFor(s => s.Notes).MaximumLength(300);
        }).OverridePropertyName("Stops");
    }

    /// <summary>Checks the default vehicle and every stop's customer exist (vehicle at the same branch).</summary>
    public static async Task<Error?> CheckReferencesAsync(IAppDbContext dbContext, string branchId, Guid? defaultVehicleId,
        List<DeliveryRouteStopInput> stops, CancellationToken ct)
    {
        if (defaultVehicleId is { } vehicleId && !await dbContext.Vehicles.AnyAsync(v => v.Id == vehicleId && v.BranchId == branchId, ct))
            return Error.NotFound("DeliveryRoute.VehicleNotFound", "The default vehicle was not found at this branch.");

        var customerIds = stops.Select(s => s.CustomerId).Distinct().ToList();
        var found = await dbContext.Customers.CountAsync(c => customerIds.Contains(c.Id), ct);
        if (found != customerIds.Count)
            return Error.NotFound("DeliveryRoute.CustomerNotFound", "One or more customers on this route were not found.");

        return null;
    }

    public static List<DeliveryRouteStop> BuildStops(List<DeliveryRouteStopInput> stops) =>
        stops.Select((s, i) => new DeliveryRouteStop { CustomerId = s.CustomerId, Sequence = i + 1, Notes = s.Notes }).ToList();

    public static IQueryable<DeliveryRoute> WithDetails(this IQueryable<DeliveryRoute> query) =>
        query.Include(r => r.DefaultVehicle).Include(r => r.Stops).ThenInclude(s => s.Customer);

    /// <summary>Callers must load DefaultVehicle and Stops.Customer (see <see cref="WithDetails"/>).</summary>
    public static DeliveryRouteResponse ToResponse(DeliveryRoute r) => new(
        r.Id, r.BranchId, r.Code, r.Name, r.Description, r.DefaultVehicleId, r.DefaultVehicle?.Code, r.Status,
        r.Stops.OrderBy(s => s.Sequence)
            .Select(s => new DeliveryRouteStopResponse(s.Id, s.CustomerId, s.Customer.Name, s.Sequence, s.Notes))
            .ToList(),
        r.CreatedAt);
}
