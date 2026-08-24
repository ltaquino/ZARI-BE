using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.Features.Workflow.Notifications.MarkRead;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .WithGroupName("Workflow")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllNotifications")
            .WithSummary("Get every notification");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateNotificationCommand>>()
            .WithName("CreateNotification")
            .WithSummary("Create a notification");

        group.MapPost("/mark-read", MarkRead)
            .AddEndpointFilter<ValidationFilter<MarkNotificationsReadCommand>>()
            .WithName("MarkNotificationsRead")
            .WithSummary("Mark one or more notifications as read for a user");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllNotificationsQuery, Result<List<NotificationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllNotificationsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateNotificationCommand command,
        ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> MarkRead(
        MarkNotificationsReadCommand command,
        ICommandHandler<MarkNotificationsReadCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
