using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.Approve;
using ZARI.Application.Features.Loan.LoanApplications.Cancel;
using ZARI.Application.Features.Loan.LoanApplications.Create;
using ZARI.Application.Features.Loan.LoanApplications.Delete;
using ZARI.Application.Features.Loan.LoanApplications.Get;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Reject;
using ZARI.Application.Features.Loan.LoanApplications.Submit;
using ZARI.Application.Features.Loan.LoanApplications.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanApplicationEndpoints
{
    public static void MapLoanApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-applications")
            .WithTags("LoanApplications")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanApplications")
            .WithSummary("Get all loan applications");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanApplicationById")
            .WithSummary("Get a loan application by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanApplicationCommand>>()
            .WithName("CreateLoanApplication")
            .WithSummary("Create a draft loan application");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanApplication")
            .WithSummary("Update a draft loan application");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanApplication")
            .WithSummary("Delete a draft loan application");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitLoanApplication")
            .WithSummary("Submit a draft loan application for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveLoanApplication")
            .WithSummary("Approve a pending loan application");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectLoanApplication")
            .WithSummary("Reject a pending loan application back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelLoanApplication")
            .WithSummary("Cancel a loan application that has not yet been superseded by a loan account");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanApplicationsQuery, Result<List<LoanApplicationResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanApplicationsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanApplicationQuery, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanApplicationQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanApplicationCommand command,
        ICommandHandler<CreateLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanApplicationById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanApplicationRequest request,
        IValidator<UpdateLoanApplicationCommand> validator,
        ICommandHandler<UpdateLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanApplicationCommand(
            id, request.BranchId, request.CustomerId, request.LoanProductId, request.ApplicationDate, request.RequestedPrincipal,
            request.RequestedTermMonths, request.Purpose, request.Remarks, request.UpdatedBy, request.Collaterals, request.CoMakers);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanApplicationCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanApplicationCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitLoanApplicationRequest request,
        IValidator<SubmitLoanApplicationCommand> validator,
        ICommandHandler<SubmitLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanApplicationCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideLoanApplicationRequest request,
        IValidator<ApproveLoanApplicationCommand> validator,
        ICommandHandler<ApproveLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLoanApplicationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideLoanApplicationRequiredCommentRequest request,
        IValidator<RejectLoanApplicationCommand> validator,
        ICommandHandler<RejectLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectLoanApplicationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelLoanApplicationRequest request,
        IValidator<CancelLoanApplicationCommand> validator,
        ICommandHandler<CancelLoanApplicationCommand, Result<LoanApplicationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelLoanApplicationCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanApplicationRequest(
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    DateTimeOffset ApplicationDate,
    decimal RequestedPrincipal,
    int RequestedTermMonths,
    string? Purpose,
    string? Remarks,
    string? UpdatedBy,
    List<LoanCollateralInput> Collaterals,
    List<LoanCoMakerInput> CoMakers);

public sealed record SubmitLoanApplicationRequest(string RequestedBy);
public sealed record DecideLoanApplicationRequest(string ApproverUserId, string? Comments);
public sealed record DecideLoanApplicationRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelLoanApplicationRequest(string CancelledBy, string Reason);
