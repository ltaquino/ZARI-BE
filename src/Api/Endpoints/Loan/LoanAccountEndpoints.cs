using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.Cancel;
using ZARI.Application.Features.Loan.LoanAccounts.Create;
using ZARI.Application.Features.Loan.LoanAccounts.Delete;
using ZARI.Application.Features.Loan.LoanAccounts.Get;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.SetDisputeStatus;
using ZARI.Application.Features.Loan.LoanAccounts.Update;
using ZARI.Application.Features.Loan.LoanLedgerEntries.GetByAccount;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanAccountEndpoints
{
    public static void MapLoanAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-accounts")
            .WithTags("LoanAccounts")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanAccounts")
            .WithSummary("Get all loan accounts");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanAccountById")
            .WithSummary("Get a loan account by ID, including its amortization schedule");

        group.MapGet("/{id:guid}/ledger", GetLedger)
            .WithName("GetLoanAccountLedger")
            .WithSummary("Get a loan account's append-only running-principal-balance ledger");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanAccountCommand>>()
            .WithName("CreateLoanAccount")
            .WithSummary("Create a loan account, generating its amortization schedule");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanAccount")
            .WithSummary("Update a loan account still pending disbursement, regenerating its schedule");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanAccount")
            .WithSummary("Delete a loan account still pending disbursement");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelLoanAccount")
            .WithSummary("Cancel a loan account still pending disbursement");

        group.MapPost("/{id:guid}/dispute-status", SetDisputeStatus)
            .WithName("SetLoanAccountDisputeStatus")
            .WithSummary("Raise or clear the CISA negative-credit-information dispute flag on a loan account");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanAccountsQuery, Result<List<LoanAccountResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanAccountsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanAccountQuery, Result<LoanAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanAccountQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetLedger(
        Guid id,
        IQueryHandler<GetLoanLedgerEntriesByAccountQuery, Result<List<LoanLedgerEntryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanLedgerEntriesByAccountQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanAccountCommand command,
        ICommandHandler<CreateLoanAccountCommand, Result<LoanAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanAccountById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanAccountRequest request,
        IValidator<UpdateLoanAccountCommand> validator,
        ICommandHandler<UpdateLoanAccountCommand, Result<LoanAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanAccountCommand(
            id, request.BranchId, request.CustomerId, request.LoanProductId, request.PrincipalAmount, request.TermMonths,
            request.GrantDate, request.FirstDueDate, request.Remarks, request.LoanReceivableAccountId,
            request.InterestIncomeAccountId, request.PenaltyIncomeAccountId, request.UpdatedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanAccountCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanAccountCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelLoanAccountRequest request,
        IValidator<CancelLoanAccountCommand> validator,
        ICommandHandler<CancelLoanAccountCommand, Result<LoanAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelLoanAccountCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> SetDisputeStatus(
        Guid id,
        SetLoanAccountDisputeStatusRequest request,
        IValidator<SetLoanAccountDisputeStatusCommand> validator,
        ICommandHandler<SetLoanAccountDisputeStatusCommand, Result<LoanAccountResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SetLoanAccountDisputeStatusCommand(id, request.IsDisputed, request.DisputeNotes, request.UpdatedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanAccountRequest(
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    decimal PrincipalAmount,
    int TermMonths,
    DateTimeOffset GrantDate,
    DateTimeOffset FirstDueDate,
    string? Remarks,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    string? UpdatedBy);

public sealed record CancelLoanAccountRequest(string CancelledBy, string Reason);
public sealed record SetLoanAccountDisputeStatusRequest(bool IsDisputed, string? DisputeNotes, string? UpdatedBy);
