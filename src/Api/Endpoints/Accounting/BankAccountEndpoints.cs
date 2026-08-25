using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.BankAccounts.Create;
using ZARI.Application.Features.Accounting.BankAccounts.Delete;
using ZARI.Application.Features.Accounting.BankAccounts.Get;
using ZARI.Application.Features.Accounting.BankAccounts.GetAll;
using ZARI.Application.Features.Accounting.BankAccounts.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class BankAccountEndpoints
{
    public static void MapBankAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bank-accounts")
            .WithTags("BankAccounts")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankAccounts")
            .WithSummary("Get all bank accounts");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetBankAccountById")
            .WithSummary("Get a bank account by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateBankAccountCommand>>()
            .WithName("CreateBankAccount")
            .WithSummary("Create a new bank account");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateBankAccount")
            .WithSummary("Update an existing bank account");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteBankAccount")
            .WithSummary("Delete a bank account");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllBankAccountsQuery, Result<List<BankAccountResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllBankAccountsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetBankAccountQuery, Result<BankAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBankAccountQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateBankAccountCommand command,
        ICommandHandler<CreateBankAccountCommand, Result<BankAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetBankAccountById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateBankAccountRequest request,
        IValidator<UpdateBankAccountCommand> validator,
        ICommandHandler<UpdateBankAccountCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBankAccountCommand(id, request.BranchId, request.AccountName, request.AccountNumber, request.BankName, request.GlAccountId, request.CurrencyId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteBankAccountCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBankAccountCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateBankAccountRequest(string BranchId, string AccountName, string AccountNumber, string BankName, Guid GlAccountId, string? CurrencyId);
