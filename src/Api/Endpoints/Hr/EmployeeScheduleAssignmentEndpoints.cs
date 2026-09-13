using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Create;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Delete;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.GetAll;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class EmployeeScheduleAssignmentEndpoints
{
    public static void MapEmployeeScheduleAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee-schedule-assignments")
            .WithTags("EmployeeScheduleAssignments")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllEmployeeScheduleAssignments").WithSummary("Get all employee schedule assignments");
        group.MapGet("/{id:guid}", GetById).WithName("GetEmployeeScheduleAssignmentById").WithSummary("Get an employee schedule assignment by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateEmployeeScheduleAssignmentCommand>>().WithName("CreateEmployeeScheduleAssignment").WithSummary("Assign a work schedule to an employee");
        group.MapPut("/{id:guid}", Update).WithName("UpdateEmployeeScheduleAssignment").WithSummary("Update an existing schedule assignment");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteEmployeeScheduleAssignment").WithSummary("Delete a schedule assignment");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllEmployeeScheduleAssignmentsQuery, Result<List<EmployeeScheduleAssignmentResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllEmployeeScheduleAssignmentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetEmployeeScheduleAssignmentQuery, Result<EmployeeScheduleAssignmentResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEmployeeScheduleAssignmentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateEmployeeScheduleAssignmentCommand command, ICommandHandler<CreateEmployeeScheduleAssignmentCommand, Result<EmployeeScheduleAssignmentResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetEmployeeScheduleAssignmentById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateEmployeeScheduleAssignmentRequest request, IValidator<UpdateEmployeeScheduleAssignmentCommand> validator, ICommandHandler<UpdateEmployeeScheduleAssignmentCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeScheduleAssignmentCommand(id, request.WorkScheduleId, request.EffectiveDate, request.EndDate);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteEmployeeScheduleAssignmentCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteEmployeeScheduleAssignmentCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateEmployeeScheduleAssignmentRequest(Guid WorkScheduleId, DateTimeOffset EffectiveDate, DateTimeOffset? EndDate);
