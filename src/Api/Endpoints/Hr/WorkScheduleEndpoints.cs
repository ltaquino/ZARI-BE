using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WorkSchedules.Create;
using ZARI.Application.Features.Hr.WorkSchedules.Delete;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Application.Features.Hr.WorkSchedules.GetAll;
using ZARI.Application.Features.Hr.WorkSchedules.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class WorkScheduleEndpoints
{
    public static void MapWorkScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-schedules")
            .WithTags("WorkSchedules")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllWorkSchedules").WithSummary("Get all work schedules");
        group.MapGet("/{id:guid}", GetById).WithName("GetWorkScheduleById").WithSummary("Get a work schedule by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateWorkScheduleCommand>>().WithName("CreateWorkSchedule").WithSummary("Create a new work schedule");
        group.MapPut("/{id:guid}", Update).WithName("UpdateWorkSchedule").WithSummary("Update an existing work schedule");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteWorkSchedule").WithSummary("Delete a work schedule");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllWorkSchedulesQuery, Result<List<WorkScheduleResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllWorkSchedulesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetWorkScheduleQuery, Result<WorkScheduleResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkScheduleQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateWorkScheduleCommand command, ICommandHandler<CreateWorkScheduleCommand, Result<WorkScheduleResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetWorkScheduleById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateWorkScheduleRequest request, IValidator<UpdateWorkScheduleCommand> validator, ICommandHandler<UpdateWorkScheduleCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateWorkScheduleCommand(id, request.Code, request.Name, request.TimeIn, request.TimeOut, request.WorkDays, request.GracePeriodMinutes, request.BreakMinutes, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteWorkScheduleCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteWorkScheduleCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateWorkScheduleRequest(string Code, string Name, TimeOnly TimeIn, TimeOnly TimeOut, string WorkDays, int GracePeriodMinutes, int BreakMinutes, string Status);
