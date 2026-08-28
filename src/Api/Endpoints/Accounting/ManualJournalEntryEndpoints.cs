using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ManualJournalEntries.ApproveCancellation;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Approve;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Cancel;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Create;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Delete;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Get;
using ZARI.Application.Features.Accounting.ManualJournalEntries.GetAll;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Reject;
using ZARI.Application.Features.Accounting.ManualJournalEntries.RejectCancellation;
using ZARI.Application.Features.Accounting.ManualJournalEntries.RequestCancellation;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Submit;
using ZARI.Application.Features.Accounting.ManualJournalEntries.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ManualJournalEntryEndpoints
{
    public static void MapManualJournalEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manual-journal-entries")
            .WithTags("ManualJournalEntries")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllManualJournalEntries")
            .WithSummary("Get all manual journal entries");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetManualJournalEntryById")
            .WithSummary("Get a manual journal entry by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateManualJournalEntryCommand>>()
            .WithName("CreateManualJournalEntry")
            .WithSummary("Create a draft manual journal entry");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateManualJournalEntry")
            .WithSummary("Update a draft manual journal entry");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteManualJournalEntry")
            .WithSummary("Delete a draft manual journal entry");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitManualJournalEntry")
            .WithSummary("Submit a draft manual journal entry for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveManualJournalEntry")
            .WithSummary("Approve a pending manual journal entry — posts its lines as a real GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectManualJournalEntry")
            .WithSummary("Reject a pending manual journal entry back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelManualJournalEntry")
            .WithSummary("Cancel a draft or pending-approval manual journal entry directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestManualJournalEntryCancellation")
            .WithSummary("Request cancellation of a posted manual journal entry");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveManualJournalEntryCancellation")
            .WithSummary("Approve a cancellation request — reverses the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectManualJournalEntryCancellation")
            .WithSummary("Reject a cancellation request — the entry stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllManualJournalEntriesQuery, Result<List<ManualJournalEntryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllManualJournalEntriesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetManualJournalEntryQuery, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetManualJournalEntryQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateManualJournalEntryCommand command,
        ICommandHandler<CreateManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetManualJournalEntryById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateManualJournalEntryRequest request,
        IValidator<UpdateManualJournalEntryCommand> validator,
        ICommandHandler<UpdateManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateManualJournalEntryCommand(id, request.EntryDate, request.Remarks, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteManualJournalEntryCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteManualJournalEntryCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitManualJournalEntryRequest request,
        IValidator<SubmitManualJournalEntryCommand> validator,
        ICommandHandler<SubmitManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitManualJournalEntryCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideManualJournalEntryRequest request,
        IValidator<ApproveManualJournalEntryCommand> validator,
        ICommandHandler<ApproveManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveManualJournalEntryCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideManualJournalEntryRequiredCommentRequest request,
        IValidator<RejectManualJournalEntryCommand> validator,
        ICommandHandler<RejectManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectManualJournalEntryCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelManualJournalEntryRequest request,
        IValidator<CancelManualJournalEntryCommand> validator,
        ICommandHandler<CancelManualJournalEntryCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelManualJournalEntryCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestManualJournalEntryCancellationRequest request,
        IValidator<RequestManualJournalEntryCancellationCommand> validator,
        ICommandHandler<RequestManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestManualJournalEntryCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideManualJournalEntryRequest request,
        IValidator<ApproveManualJournalEntryCancellationCommand> validator,
        ICommandHandler<ApproveManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveManualJournalEntryCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideManualJournalEntryRequiredCommentRequest request,
        IValidator<RejectManualJournalEntryCancellationCommand> validator,
        ICommandHandler<RejectManualJournalEntryCancellationCommand, Result<ManualJournalEntryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectManualJournalEntryCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateManualJournalEntryRequest(
    DateTimeOffset EntryDate,
    string Remarks,
    string? UpdatedBy,
    List<ManualJournalEntryLineInput> Lines);

public sealed record SubmitManualJournalEntryRequest(string RequestedBy);
public sealed record DecideManualJournalEntryRequest(string ApproverUserId, string? Comments);
public sealed record DecideManualJournalEntryRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelManualJournalEntryRequest(string CancelledBy, string Reason);
public sealed record RequestManualJournalEntryCancellationRequest(string RequestedBy, string Reason);
