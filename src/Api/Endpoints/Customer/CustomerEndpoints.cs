using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Create;
using ZARI.Application.Features.Customers.Delete;
using ZARI.Application.Features.Customers.Get;
using ZARI.Application.Features.Customers.GetAll;
using ZARI.Application.Features.Customers.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .WithGroupName("Customer")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCustomers")
            .WithSummary("Get all customers");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCustomerById")
            .WithSummary("Get a customer by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCustomerCommand>>()
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateCustomer")
            .WithSummary("Update an existing customer");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCustomer")
            .WithSummary("Delete a customer");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCustomersQuery, Result<List<CustomerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCustomersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCustomerQuery, Result<CustomerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCustomerCommand command,
        ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCustomerById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCustomerRequest request,
        IValidator<UpdateCustomerCommand> validator,
        ICommandHandler<UpdateCustomerCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(id, request.Name, request.Type, request.Email, request.Phone,
            request.BranchId, request.Status, request.Owner, request.Address, request.Notes);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteCustomerCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCustomerCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateCustomerRequest(
    string Name,
    string Type,
    string Email,
    string Phone,
    string BranchId,
    string Status,
    string Owner,
    string Address,
    string? Notes);
