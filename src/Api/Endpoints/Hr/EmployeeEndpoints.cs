using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Employees.Create;
using ZARI.Application.Features.Hr.Employees.Delete;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Application.Features.Hr.Employees.GetAll;
using ZARI.Application.Features.Hr.Employees.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employees")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllEmployees").WithSummary("Get all employees");
        group.MapGet("/{id:guid}", GetById).WithName("GetEmployeeById").WithSummary("Get an employee by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateEmployeeCommand>>().WithName("CreateEmployee").WithSummary("Create a new employee");
        group.MapPut("/{id:guid}", Update).WithName("UpdateEmployee").WithSummary("Update an existing employee");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteEmployee").WithSummary("Delete an employee");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllEmployeesQuery, Result<List<EmployeeResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllEmployeesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetEmployeeQuery, Result<EmployeeResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEmployeeQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateEmployeeCommand command, ICommandHandler<CreateEmployeeCommand, Result<EmployeeResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetEmployeeById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateEmployeeRequest request, IValidator<UpdateEmployeeCommand> validator, ICommandHandler<UpdateEmployeeCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeCommand(id, request.EmployeeNo, request.FirstName, request.MiddleName, request.LastName, request.Suffix,
            request.DateOfBirth, request.Sex, request.CivilStatus, request.Address, request.BranchId, request.PositionId, request.DepartmentId,
            request.DateHired, request.Status, request.EmploymentStatus, request.RegularizationDate, request.SeparationDate, request.SeparationReason,
            request.SssNo, request.PhilHealthNo, request.PagIbigNo, request.Tin, request.BasicSalary, request.BankAccountNo, request.BankName,
            request.BarcodeNo, request.RfidTagNo, request.ApplicationUserId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteEmployeeCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteEmployeeCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateEmployeeRequest(
    string EmployeeNo,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Suffix,
    DateTimeOffset? DateOfBirth,
    string? Sex,
    string? CivilStatus,
    string? Address,
    string BranchId,
    Guid PositionId,
    Guid DepartmentId,
    DateTimeOffset DateHired,
    string Status,
    string EmploymentStatus,
    DateTimeOffset? RegularizationDate,
    DateTimeOffset? SeparationDate,
    string? SeparationReason,
    string? SssNo,
    string? PhilHealthNo,
    string? PagIbigNo,
    string? Tin,
    decimal BasicSalary,
    string? BankAccountNo,
    string? BankName,
    string? BarcodeNo,
    string? RfidTagNo,
    string? ApplicationUserId);
