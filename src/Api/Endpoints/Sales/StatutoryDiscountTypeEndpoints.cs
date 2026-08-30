using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Create;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Delete;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.GetAll;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class StatutoryDiscountTypeEndpoints
{
    public static void MapStatutoryDiscountTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/statutory-discount-types")
            .WithTags("StatutoryDiscountTypes")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllStatutoryDiscountTypes")
            .WithSummary("Get all statutory discount types");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetStatutoryDiscountTypeById")
            .WithSummary("Get a statutory discount type by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateStatutoryDiscountTypeCommand>>()
            .WithName("CreateStatutoryDiscountType")
            .WithSummary("Create a new statutory discount type");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateStatutoryDiscountType")
            .WithSummary("Update an existing statutory discount type");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteStatutoryDiscountType")
            .WithSummary("Delete a statutory discount type");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStatutoryDiscountTypesQuery, Result<List<StatutoryDiscountTypeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStatutoryDiscountTypesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStatutoryDiscountTypeQuery, Result<StatutoryDiscountTypeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStatutoryDiscountTypeQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStatutoryDiscountTypeCommand command,
        ICommandHandler<CreateStatutoryDiscountTypeCommand, Result<StatutoryDiscountTypeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetStatutoryDiscountTypeById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateStatutoryDiscountTypeRequest request,
        IValidator<UpdateStatutoryDiscountTypeCommand> validator,
        ICommandHandler<UpdateStatutoryDiscountTypeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStatutoryDiscountTypeCommand(id, request.Code, request.Name, request.DiscountPct, request.IsVatExempt, request.RequiredIdLabel, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteStatutoryDiscountTypeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteStatutoryDiscountTypeCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateStatutoryDiscountTypeRequest(string Code, string Name, decimal DiscountPct, bool IsVatExempt, string RequiredIdLabel, string Status);
