using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.CreditRecords.Create;
using ZARI.Application.Features.Customers.CreditRecords.Delete;
using ZARI.Application.Features.Customers.CreditRecords.Get;
using ZARI.Application.Features.Customers.CreditRecords.GetAll;
using ZARI.Application.Features.Customers.CreditRecords.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CustomerCreditRecordEndpoints
{
    public static void MapCustomerCreditRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer-credit-records")
            .WithTags("CustomerCreditRecords")
            .WithGroupName("Customer")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCustomerCreditRecords")
            .WithSummary("Get all member credit records (negative credit information), optionally filtered by customer");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCustomerCreditRecordById")
            .WithSummary("Get a member credit record by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCustomerCreditRecordCommand>>()
            .WithName("CreateCustomerCreditRecord")
            .WithSummary("Record a piece of negative credit information for a member");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateCustomerCreditRecord")
            .WithSummary("Update a member credit record");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCustomerCreditRecord")
            .WithSummary("Delete a member credit record");
    }

    private static async Task<IResult> GetAll(
        Guid? customerId,
        IQueryHandler<GetAllCustomerCreditRecordsQuery, Result<List<CustomerCreditRecordResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCustomerCreditRecordsQuery(customerId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCustomerCreditRecordQuery, Result<CustomerCreditRecordResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerCreditRecordQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCustomerCreditRecordCommand command,
        ICommandHandler<CreateCustomerCreditRecordCommand, Result<CustomerCreditRecordResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCustomerCreditRecordById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCustomerCreditRecordRequest request,
        IValidator<UpdateCustomerCreditRecordCommand> validator,
        ICommandHandler<UpdateCustomerCreditRecordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCreditRecordCommand(id, request.RecordType, request.Description, request.RecordDate, request.Amount, request.Remarks);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteCustomerCreditRecordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCustomerCreditRecordCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateCustomerCreditRecordRequest(string RecordType, string Description, DateTimeOffset RecordDate, decimal? Amount, string? Remarks);
