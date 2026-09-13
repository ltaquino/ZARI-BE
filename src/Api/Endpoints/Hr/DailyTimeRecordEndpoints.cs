using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Create;
using ZARI.Application.Features.Hr.DailyTimeRecords.Delete;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Application.Features.Hr.DailyTimeRecords.GetAll;
using ZARI.Application.Features.Hr.DailyTimeRecords.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DailyTimeRecordEndpoints
{
    public static void MapDailyTimeRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/daily-time-records")
            .WithTags("DailyTimeRecords")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllDailyTimeRecords").WithSummary("Get all daily time records");
        group.MapGet("/{id:guid}", GetById).WithName("GetDailyTimeRecordById").WithSummary("Get a daily time record by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateDailyTimeRecordCommand>>().WithName("CreateDailyTimeRecord").WithSummary("Create a new daily time record");
        group.MapPut("/{id:guid}", Update).WithName("UpdateDailyTimeRecord").WithSummary("Update an existing daily time record");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteDailyTimeRecord").WithSummary("Delete a daily time record");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllDailyTimeRecordsQuery, Result<List<DailyTimeRecordResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDailyTimeRecordsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetDailyTimeRecordQuery, Result<DailyTimeRecordResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDailyTimeRecordQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateDailyTimeRecordCommand command, ICommandHandler<CreateDailyTimeRecordCommand, Result<DailyTimeRecordResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetDailyTimeRecordById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateDailyTimeRecordRequest request, IValidator<UpdateDailyTimeRecordCommand> validator, ICommandHandler<UpdateDailyTimeRecordCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateDailyTimeRecordCommand(id, request.TimeIn, request.TimeOut, request.BreakOutTime, request.BreakInTime, request.Source, request.Remarks);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteDailyTimeRecordCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDailyTimeRecordCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateDailyTimeRecordRequest(DateTimeOffset? TimeIn, DateTimeOffset? TimeOut, DateTimeOffset? BreakOutTime, DateTimeOffset? BreakInTime, string Source, string? Remarks);
