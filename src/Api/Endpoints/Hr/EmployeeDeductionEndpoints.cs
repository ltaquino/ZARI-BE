using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeDeductions.Create;
using ZARI.Application.Features.Hr.EmployeeDeductions.Delete;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Application.Features.Hr.EmployeeDeductions.GetAll;
using ZARI.Application.Features.Hr.EmployeeDeductions.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class EmployeeDeductionEndpoints
{
    public static void MapEmployeeDeductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee-deductions")
            .WithTags("EmployeeDeductions")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllEmployeeDeductions").WithSummary("Get all employee deductions");
        group.MapGet("/{id:guid}", GetById).WithName("GetEmployeeDeductionById").WithSummary("Get an employee deduction by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateEmployeeDeductionCommand>>().WithName("CreateEmployeeDeduction").WithSummary("Create a new employee deduction");
        group.MapPut("/{id:guid}", Update).WithName("UpdateEmployeeDeduction").WithSummary("Update an existing employee deduction");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteEmployeeDeduction").WithSummary("Delete an employee deduction");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllEmployeeDeductionsQuery, Result<List<EmployeeDeductionResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllEmployeeDeductionsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetEmployeeDeductionQuery, Result<EmployeeDeductionResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEmployeeDeductionQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateEmployeeDeductionCommand command, ICommandHandler<CreateEmployeeDeductionCommand, Result<EmployeeDeductionResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetEmployeeDeductionById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateEmployeeDeductionRequest request, IValidator<UpdateEmployeeDeductionCommand> validator, ICommandHandler<UpdateEmployeeDeductionCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeDeductionCommand(id, request.Code, request.Name, request.Amount, request.EffectiveDate, request.EndDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteEmployeeDeductionCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteEmployeeDeductionCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateEmployeeDeductionRequest(string Code, string Name, decimal Amount, DateTimeOffset EffectiveDate, DateTimeOffset? EndDate, string Status);
