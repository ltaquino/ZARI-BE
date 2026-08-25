using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Roles.Create;
using ZARI.Application.Features.Identity.Roles.Delete;
using ZARI.Application.Features.Identity.Roles.Get;
using ZARI.Application.Features.Identity.Roles.GetAll;
using ZARI.Application.Features.Identity.Roles.Shared;
using ZARI.Application.Features.Identity.Roles.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles")
            .WithGroupName("Identity")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllRoles")
            .WithSummary("Get all roles with their form permission templates");

        group.MapGet("/{id}", GetById)
            .WithName("GetRoleById")
            .WithSummary("Get a role by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateRoleCommand>>()
            .WithName("CreateRole")
            .WithSummary("Create a new role with its form permission template");

        group.MapPut("/{id}", Update)
            .WithName("UpdateRole")
            .WithSummary("Rename a role and replace its form permission template");

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteRole")
            .WithSummary("Delete a role");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllRolesQuery, Result<List<RoleResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllRolesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        string id,
        IQueryHandler<GetRoleQuery, Result<RoleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetRoleQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateRoleCommand command,
        ICommandHandler<CreateRoleCommand, Result<RoleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetRoleById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        string id,
        UpdateRoleRequest request,
        IValidator<UpdateRoleCommand> validator,
        ICommandHandler<UpdateRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(id, request.Name, request.Permissions);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        string id,
        ICommandHandler<DeleteRoleCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteRoleCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateRoleRequest(string Name, List<FormPermissionInput> Permissions);
