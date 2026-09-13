using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Departments.Create;
using ZARI.Application.Features.Hr.Departments.Delete;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Application.Features.Hr.Departments.GetAll;
using ZARI.Application.Features.Hr.Departments.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DepartmentEndpoints
{
    public static void MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/departments")
            .WithTags("Departments")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllDepartments").WithSummary("Get all departments");
        group.MapGet("/{id:guid}", GetById).WithName("GetDepartmentById").WithSummary("Get a department by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateDepartmentCommand>>().WithName("CreateDepartment").WithSummary("Create a new department");
        group.MapPut("/{id:guid}", Update).WithName("UpdateDepartment").WithSummary("Update an existing department");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteDepartment").WithSummary("Delete a department");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllDepartmentsQuery, Result<List<DepartmentResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDepartmentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetDepartmentQuery, Result<DepartmentResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDepartmentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateDepartmentCommand command, ICommandHandler<CreateDepartmentCommand, Result<DepartmentResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetDepartmentById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateDepartmentRequest request, IValidator<UpdateDepartmentCommand> validator, ICommandHandler<UpdateDepartmentCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateDepartmentCommand(id, request.Code, request.Name, request.Status, request.HeadEmployeeId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteDepartmentCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDepartmentCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateDepartmentRequest(string Code, string Name, string Status, Guid? HeadEmployeeId);
