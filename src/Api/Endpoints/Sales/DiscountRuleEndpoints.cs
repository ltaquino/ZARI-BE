using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.DiscountRules.Create;
using ZARI.Application.Features.Sales.DiscountRules.Delete;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Application.Features.Sales.DiscountRules.GetAll;
using ZARI.Application.Features.Sales.DiscountRules.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DiscountRuleEndpoints
{
    public static void MapDiscountRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/discount-rules")
            .WithTags("DiscountRules")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllDiscountRules")
            .WithSummary("Get all discount rules");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDiscountRuleById")
            .WithSummary("Get a discount rule by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDiscountRuleCommand>>()
            .WithName("CreateDiscountRule")
            .WithSummary("Create a new discount rule");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateDiscountRule")
            .WithSummary("Update an existing discount rule");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteDiscountRule")
            .WithSummary("Delete a discount rule");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllDiscountRulesQuery, Result<List<DiscountRuleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDiscountRulesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDiscountRuleQuery, Result<DiscountRuleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDiscountRuleQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateDiscountRuleCommand command,
        ICommandHandler<CreateDiscountRuleCommand, Result<DiscountRuleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetDiscountRuleById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateDiscountRuleRequest request,
        IValidator<UpdateDiscountRuleCommand> validator,
        ICommandHandler<UpdateDiscountRuleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDiscountRuleCommand(id, request.Code, request.Name, request.Scope, request.ItemId, request.ItemCategoryId,
            request.DiscountType, request.DiscountValue, request.MinQty, request.StartDate, request.EndDate, request.BranchId, request.Priority, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteDiscountRuleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDiscountRuleCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateDiscountRuleRequest(
    string Code,
    string Name,
    string Scope,
    Guid? ItemId,
    Guid? ItemCategoryId,
    string DiscountType,
    decimal DiscountValue,
    decimal? MinQty,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? BranchId,
    int Priority,
    string Status);
