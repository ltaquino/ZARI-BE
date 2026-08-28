using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.ApproveCancellation;
using ZARI.Application.Features.Purchasing.ApInvoices.Approve;
using ZARI.Application.Features.Purchasing.ApInvoices.Cancel;
using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.Purchasing.ApInvoices.Delete;
using ZARI.Application.Features.Purchasing.ApInvoices.Get;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Reject;
using ZARI.Application.Features.Purchasing.ApInvoices.RejectCancellation;
using ZARI.Application.Features.Purchasing.ApInvoices.RequestCancellation;
using ZARI.Application.Features.Purchasing.ApInvoices.Submit;
using ZARI.Application.Features.Purchasing.ApInvoices.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ApInvoiceEndpoints
{
    public static void MapApInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ap-invoices")
            .WithTags("ApInvoices")
            .WithGroupName("Purchasing")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllApInvoices")
            .WithSummary("Get all AP invoices");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetApInvoiceById")
            .WithSummary("Get an AP invoice by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateApInvoiceCommand>>()
            .WithName("CreateApInvoice")
            .WithSummary("Create a draft AP invoice against a posted goods receipt (PO)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateApInvoice")
            .WithSummary("Update a draft AP invoice");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteApInvoice")
            .WithSummary("Delete a draft AP invoice");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitApInvoice")
            .WithSummary("Submit a draft AP invoice for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveApInvoice")
            .WithSummary("Approve a pending AP invoice — posts the GRNI-to-AP GL journal");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectApInvoice")
            .WithSummary("Reject a pending AP invoice back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelApInvoice")
            .WithSummary("Cancel a draft or pending-approval AP invoice directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestApInvoiceCancellation")
            .WithSummary("Request cancellation of a posted AP invoice");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveApInvoiceCancellation")
            .WithSummary("Approve a cancellation request — reverses the GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectApInvoiceCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllApInvoicesQuery, Result<List<ApInvoiceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllApInvoicesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetApInvoiceQuery, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetApInvoiceQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateApInvoiceCommand command,
        ICommandHandler<CreateApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetApInvoiceById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateApInvoiceRequest request,
        IValidator<UpdateApInvoiceCommand> validator,
        ICommandHandler<UpdateApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateApInvoiceCommand(
            id, request.SupplierInvoiceNo, request.InvoiceDate, request.DueDate, request.Remarks, request.CostCenterId, request.UpdatedBy, request.Lines, request.ExpenseLines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteApInvoiceCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteApInvoiceCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitApInvoiceRequest request,
        IValidator<SubmitApInvoiceCommand> validator,
        ICommandHandler<SubmitApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitApInvoiceCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideApInvoiceRequest request,
        IValidator<ApproveApInvoiceCommand> validator,
        ICommandHandler<ApproveApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveApInvoiceCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideApInvoiceRequiredCommentRequest request,
        IValidator<RejectApInvoiceCommand> validator,
        ICommandHandler<RejectApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectApInvoiceCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelApInvoiceRequest request,
        IValidator<CancelApInvoiceCommand> validator,
        ICommandHandler<CancelApInvoiceCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelApInvoiceCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestApInvoiceCancellationRequest request,
        IValidator<RequestApInvoiceCancellationCommand> validator,
        ICommandHandler<RequestApInvoiceCancellationCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestApInvoiceCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideApInvoiceRequest request,
        IValidator<ApproveApInvoiceCancellationCommand> validator,
        ICommandHandler<ApproveApInvoiceCancellationCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveApInvoiceCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideApInvoiceRequiredCommentRequest request,
        IValidator<RejectApInvoiceCancellationCommand> validator,
        ICommandHandler<RejectApInvoiceCancellationCommand, Result<ApInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectApInvoiceCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateApInvoiceRequest(
    string SupplierInvoiceNo,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<ApInvoiceLineInput> Lines,
    List<ApInvoiceExpenseLineInput> ExpenseLines);

public sealed record SubmitApInvoiceRequest(string RequestedBy);
public sealed record DecideApInvoiceRequest(string ApproverUserId, string? Comments);
public sealed record DecideApInvoiceRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelApInvoiceRequest(string CancelledBy, string Reason);
public sealed record RequestApInvoiceCancellationRequest(string RequestedBy, string Reason);
