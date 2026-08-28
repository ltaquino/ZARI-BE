using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Suppliers.Create;
using ZARI.Application.Features.Purchasing.Suppliers.Delete;
using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Application.Features.Purchasing.Suppliers.GetAll;
using ZARI.Application.Features.Purchasing.Suppliers.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/suppliers")
            .WithTags("Suppliers")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSuppliers")
            .WithSummary("Get all suppliers");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetSupplierById")
            .WithSummary("Get a supplier by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateSupplierCommand>>()
            .WithName("CreateSupplier")
            .WithSummary("Create a new supplier");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateSupplier")
            .WithSummary("Update an existing supplier");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteSupplier")
            .WithSummary("Delete a supplier");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSuppliersQuery, Result<List<SupplierResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSuppliersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetSupplierQuery, Result<SupplierResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSupplierQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateSupplierCommand command,
        ICommandHandler<CreateSupplierCommand, Result<SupplierResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetSupplierById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateSupplierRequest request,
        IValidator<UpdateSupplierCommand> validator,
        ICommandHandler<UpdateSupplierCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSupplierCommand(id, request.Code, request.Name, request.TaxId, request.PaymentTermsDays,
            request.CurrencyId, request.ApAccountId, request.Address, request.ContactPerson, request.ContactNumber,
            request.Email, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteSupplierCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteSupplierCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateSupplierRequest(
    string Code,
    string Name,
    string? TaxId,
    int? PaymentTermsDays,
    string? CurrencyId,
    Guid? ApAccountId,
    string? Address,
    string? ContactPerson,
    string? ContactNumber,
    string? Email,
    string Status);
