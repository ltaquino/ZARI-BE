using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.DeviceSettings;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DeviceSettingsEndpoints
{
    public static void MapDeviceSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/device-settings")
            .WithTags("DeviceSettings")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", Get)
            .WithName("GetDeviceSettings")
            .WithSummary("Company/branch facts an offline device needs that its user can't read from the admin screens");
    }

    private static async Task<IResult> Get(
        IQueryHandler<GetDeviceSettingsQuery, Result<DeviceSettingsResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDeviceSettingsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
