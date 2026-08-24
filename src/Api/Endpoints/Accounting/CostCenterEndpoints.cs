using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.CostCenters.Create;
using ZARI.Application.Features.Accounting.CostCenters.Delete;
using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Application.Features.Accounting.CostCenters.GetAll;
using ZARI.Application.Features.Accounting.CostCenters.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CostCenterEndpoints
{
    public static void MapCostCenterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cost-centers")
            .WithTags("CostCenters")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCostCenters")
            .WithSummary("Get all cost centers");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCostCenterById")
            .WithSummary("Get a cost center by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCostCenterCommand>>()
            .WithName("CreateCostCenter")
            .WithSummary("Create a new cost center");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateCostCenter")
            .WithSummary("Update an existing cost center");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCostCenter")
            .WithSummary("Delete a cost center");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCostCentersQuery, Result<List<CostCenterResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCostCentersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCostCenterQuery, Result<CostCenterResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCostCenterQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCostCenterCommand command,
        ICommandHandler<CreateCostCenterCommand, Result<CostCenterResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCostCenterById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCostCenterRequest request,
        IValidator<UpdateCostCenterCommand> validator,
        ICommandHandler<UpdateCostCenterCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCostCenterCommand(id, request.BranchId, request.Code, request.Name, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteCostCenterCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCostCenterCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateCostCenterRequest(string? BranchId, string Code, string Name, string Status);
