using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Branches.Create;
using ZARI.Application.Features.SystemModule.Branches.Delete;
using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Application.Features.SystemModule.Branches.GetAll;
using ZARI.Application.Features.SystemModule.Branches.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class BranchEndpoints
{
    public static void MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/branches")
            .WithTags("Branches")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBranches")
            .WithSummary("Get all branches");

        group.MapGet("/{id}", GetById)
            .WithName("GetBranchById")
            .WithSummary("Get a branch by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateBranchCommand>>()
            .WithName("CreateBranch")
            .WithSummary("Create a new branch");

        group.MapPut("/{id}", Update)
            .WithName("UpdateBranch")
            .WithSummary("Update an existing branch");

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteBranch")
            .WithSummary("Delete a branch");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllBranchesQuery, Result<List<BranchResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllBranchesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        string id,
        IQueryHandler<GetBranchQuery, Result<BranchResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBranchQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateBranchCommand command,
        ICommandHandler<CreateBranchCommand, Result<BranchResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetBranchById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        string id,
        UpdateBranchRequest request,
        IValidator<UpdateBranchCommand> validator,
        ICommandHandler<UpdateBranchCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBranchCommand(
            id, request.Name, request.Code, request.City, request.Address, request.Phone, request.Status, request.IsHeadOffice,
            request.BirBranchCode, request.PosPermitNumber, request.PosPermitDateIssued, request.MachineIdentificationNumber, request.MachineSerialNumber);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        string id,
        ICommandHandler<DeleteBranchCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBranchCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateBranchRequest(
    string Name,
    string Code,
    string City,
    string Address,
    string Phone,
    string Status,
    bool IsHeadOffice,
    string? BirBranchCode,
    string? PosPermitNumber,
    DateTime? PosPermitDateIssued,
    string? MachineIdentificationNumber,
    string? MachineSerialNumber);
