using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlAccounts.Create;
using ZARI.Application.Features.Accounting.GlAccounts.Delete;
using ZARI.Application.Features.Accounting.GlAccounts.Get;
using ZARI.Application.Features.Accounting.GlAccounts.GetAll;
using ZARI.Application.Features.Accounting.GlAccounts.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GlAccountEndpoints
{
    public static void MapGlAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gl-accounts")
            .WithTags("GlAccounts")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGlAccounts")
            .WithSummary("Get all GL accounts");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetGlAccountById")
            .WithSummary("Get a GL account by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateGlAccountCommand>>()
            .WithName("CreateGlAccount")
            .WithSummary("Create a new GL account");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateGlAccount")
            .WithSummary("Update an existing GL account");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteGlAccount")
            .WithSummary("Delete a GL account");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGlAccountsQuery, Result<List<GlAccountResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGlAccountsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetGlAccountQuery, Result<GlAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGlAccountQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateGlAccountCommand command,
        ICommandHandler<CreateGlAccountCommand, Result<GlAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetGlAccountById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateGlAccountRequest request,
        IValidator<UpdateGlAccountCommand> validator,
        ICommandHandler<UpdateGlAccountCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGlAccountCommand(id, request.Code, request.Name, request.AccountType, request.NormalBalance, request.ParentAccountId, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteGlAccountCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGlAccountCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateGlAccountRequest(string Code, string Name, string AccountType, string NormalBalance, Guid? ParentAccountId, string Status);
