using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeAllowances.Create;
using ZARI.Application.Features.Hr.EmployeeAllowances.Delete;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Application.Features.Hr.EmployeeAllowances.GetAll;
using ZARI.Application.Features.Hr.EmployeeAllowances.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class EmployeeAllowanceEndpoints
{
    public static void MapEmployeeAllowanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee-allowances")
            .WithTags("EmployeeAllowances")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllEmployeeAllowances").WithSummary("Get all employee allowances");
        group.MapGet("/{id:guid}", GetById).WithName("GetEmployeeAllowanceById").WithSummary("Get an employee allowance by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateEmployeeAllowanceCommand>>().WithName("CreateEmployeeAllowance").WithSummary("Create a new employee allowance");
        group.MapPut("/{id:guid}", Update).WithName("UpdateEmployeeAllowance").WithSummary("Update an existing employee allowance");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteEmployeeAllowance").WithSummary("Delete an employee allowance");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllEmployeeAllowancesQuery, Result<List<EmployeeAllowanceResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllEmployeeAllowancesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetEmployeeAllowanceQuery, Result<EmployeeAllowanceResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEmployeeAllowanceQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateEmployeeAllowanceCommand command, ICommandHandler<CreateEmployeeAllowanceCommand, Result<EmployeeAllowanceResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetEmployeeAllowanceById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateEmployeeAllowanceRequest request, IValidator<UpdateEmployeeAllowanceCommand> validator, ICommandHandler<UpdateEmployeeAllowanceCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeAllowanceCommand(id, request.Code, request.Name, request.Amount, request.EffectiveDate, request.EndDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteEmployeeAllowanceCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteEmployeeAllowanceCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateEmployeeAllowanceRequest(string Code, string Name, decimal Amount, DateTimeOffset EffectiveDate, DateTimeOffset? EndDate, string Status);
