using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveTypes.Create;
using ZARI.Application.Features.Hr.LeaveTypes.Delete;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Application.Features.Hr.LeaveTypes.GetAll;
using ZARI.Application.Features.Hr.LeaveTypes.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LeaveTypeEndpoints
{
    public static void MapLeaveTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leave-types")
            .WithTags("LeaveTypes")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllLeaveTypes").WithSummary("Get all leave types");
        group.MapGet("/{id:guid}", GetById).WithName("GetLeaveTypeById").WithSummary("Get a leave type by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateLeaveTypeCommand>>().WithName("CreateLeaveType").WithSummary("Create a new leave type");
        group.MapPut("/{id:guid}", Update).WithName("UpdateLeaveType").WithSummary("Update an existing leave type");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteLeaveType").WithSummary("Delete a leave type");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllLeaveTypesQuery, Result<List<LeaveTypeResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLeaveTypesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetLeaveTypeQuery, Result<LeaveTypeResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLeaveTypeQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateLeaveTypeCommand command, ICommandHandler<CreateLeaveTypeCommand, Result<LeaveTypeResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetLeaveTypeById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateLeaveTypeRequest request, IValidator<UpdateLeaveTypeCommand> validator, ICommandHandler<UpdateLeaveTypeCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateLeaveTypeCommand(id, request.Code, request.Name, request.AccrualFrequency, request.AccrualAmount, request.MaxCarryOverDays, request.IsPaid, request.RequiresDocumentation, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteLeaveTypeCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLeaveTypeCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateLeaveTypeRequest(string Code, string Name, string AccrualFrequency, decimal AccrualAmount, decimal? MaxCarryOverDays, bool IsPaid, bool RequiresDocumentation, string Status);
