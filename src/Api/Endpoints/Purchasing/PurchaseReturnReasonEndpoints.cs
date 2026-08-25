using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Create;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Delete;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PurchaseReturnReasonEndpoints
{
    public static void MapPurchaseReturnReasonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchase-return-reasons")
            .WithTags("PurchaseReturnReasons")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPurchaseReturnReasons")
            .WithSummary("Get all purchase return reasons");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPurchaseReturnReasonById")
            .WithSummary("Get a purchase return reason by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePurchaseReturnReasonCommand>>()
            .WithName("CreatePurchaseReturnReason")
            .WithSummary("Create a new purchase return reason");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePurchaseReturnReason")
            .WithSummary("Update an existing purchase return reason");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePurchaseReturnReason")
            .WithSummary("Delete a purchase return reason");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllPurchaseReturnReasonsQuery, Result<List<PurchaseReturnReasonResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPurchaseReturnReasonsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPurchaseReturnReasonQuery, Result<PurchaseReturnReasonResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPurchaseReturnReasonQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePurchaseReturnReasonCommand command,
        ICommandHandler<CreatePurchaseReturnReasonCommand, Result<PurchaseReturnReasonResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPurchaseReturnReasonById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePurchaseReturnReasonRequest request,
        IValidator<UpdatePurchaseReturnReasonCommand> validator,
        ICommandHandler<UpdatePurchaseReturnReasonCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePurchaseReturnReasonCommand(id, request.Code, request.Description, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePurchaseReturnReasonCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePurchaseReturnReasonCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdatePurchaseReturnReasonRequest(string Code, string? Description, string Status);
