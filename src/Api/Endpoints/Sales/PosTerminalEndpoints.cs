using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Create;
using ZARI.Application.Features.Sales.PosTerminals.Delete;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Application.Features.Sales.PosTerminals.GetAll;
using ZARI.Application.Features.Sales.PosTerminals.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PosTerminalEndpoints
{
    public static void MapPosTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos-terminals")
            .WithTags("PosTerminals")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPosTerminals")
            .WithSummary("Get all POS terminals, optionally filtered by branch");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPosTerminalById")
            .WithSummary("Get a POS terminal by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePosTerminalCommand>>()
            .WithName("CreatePosTerminal")
            .WithSummary("Create a new POS terminal");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePosTerminal")
            .WithSummary("Update an existing POS terminal");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePosTerminal")
            .WithSummary("Delete a POS terminal");
    }

    private static async Task<IResult> GetAll(
        string? branchId,
        IQueryHandler<GetAllPosTerminalsQuery, Result<List<PosTerminalResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPosTerminalsQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPosTerminalQuery, Result<PosTerminalResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPosTerminalQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePosTerminalCommand command,
        ICommandHandler<CreatePosTerminalCommand, Result<PosTerminalResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPosTerminalById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePosTerminalRequest request,
        IValidator<UpdatePosTerminalCommand> validator,
        ICommandHandler<UpdatePosTerminalCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePosTerminalCommand(id, request.Code, request.Name, request.MachineIdentificationNumber, request.MachineSerialNumber, request.PosPermitNumber, request.PosPermitDateIssued, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePosTerminalCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePosTerminalCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdatePosTerminalRequest(string Code, string Name, string? MachineIdentificationNumber, string? MachineSerialNumber, string? PosPermitNumber, DateTime? PosPermitDateIssued, string Status);
