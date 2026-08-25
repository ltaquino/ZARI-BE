using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Users.Create;
using ZARI.Application.Features.Identity.Users.Delete;
using ZARI.Application.Features.Identity.Users.Get;
using ZARI.Application.Features.Identity.Users.GetAll;
using ZARI.Application.Features.Identity.Users.Permissions.GetEffective;
using ZARI.Application.Features.Identity.Users.Permissions.SetOverrides;
using ZARI.Application.Features.Identity.Users.Shared;
using ZARI.Application.Features.Identity.Users.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithGroupName("Identity")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllUsers")
            .WithSummary("Get all users");

        group.MapGet("/{id}", GetById)
            .WithName("GetUserById")
            .WithSummary("Get a user by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateUserCommand>>()
            .WithName("CreateUser")
            .WithSummary("Create a new user");

        group.MapPut("/{id}", Update)
            .WithName("UpdateUser")
            .WithSummary("Update an existing user's profile, roles, and branch assignments");

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteUser")
            .WithSummary("Delete a user");

        group.MapGet("/{id}/permissions", GetEffectivePermissions)
            .WithName("GetUserEffectivePermissions")
            .WithSummary("Get a user's effective per-form permissions (per-user override if set, else the OR of their roles)");

        group.MapPut("/{id}/permissions", SetPermissionOverrides)
            .WithName("SetUserPermissionOverrides")
            .WithSummary("Replace a user's per-form permission overrides");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllUsersQuery, Result<List<UserResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllUsersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        string id,
        IQueryHandler<GetUserQuery, Result<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateUserCommand command,
        ICommandHandler<CreateUserCommand, Result<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetUserById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        string id,
        UpdateUserRequest request,
        IValidator<UpdateUserCommand> validator,
        ICommandHandler<UpdateUserCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.Email, request.FirstName, request.LastName, request.Phone, request.Status, request.RoleIds, request.BranchIds);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        string id,
        ICommandHandler<DeleteUserCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteUserCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> GetEffectivePermissions(
        string id,
        IQueryHandler<GetEffectiveUserPermissionsQuery, Result<List<FormPermissionResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEffectiveUserPermissionsQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> SetPermissionOverrides(
        string id,
        List<FormPermissionInput> overrides,
        IValidator<SetUserPermissionOverridesCommand> validator,
        ICommandHandler<SetUserPermissionOverridesCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new SetUserPermissionOverridesCommand(id, overrides);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateUserRequest(string Email, string FirstName, string LastName, string? Phone, string Status, List<string> RoleIds, List<string> BranchIds);
