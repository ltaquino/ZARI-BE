using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.ApproveCancellation;
using ZARI.Application.Features.Sales.SalesInvoices.Approve;
using ZARI.Application.Features.Sales.SalesInvoices.Cancel;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Application.Features.Sales.SalesInvoices.Delete;
using ZARI.Application.Features.Sales.SalesInvoices.Get;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.GetAllPaged;
using ZARI.Application.Features.Sales.SalesInvoices.RecordPrint;
using ZARI.Application.Features.Sales.SalesInvoices.Reject;
using ZARI.Application.Features.Sales.SalesInvoices.RejectCancellation;
using ZARI.Application.Features.Sales.SalesInvoices.RequestCancellation;
using ZARI.Application.Features.Sales.SalesInvoices.Submit;
using ZARI.Application.Features.Sales.SalesInvoices.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SalesInvoiceEndpoints
{
    public static void MapSalesInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales-invoices")
            .WithTags("SalesInvoices")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSalesInvoices")
            .WithSummary("Get all sales invoices");

        group.MapGet("/paged", GetAllPaged)
            .WithName("GetAllSalesInvoicesPaged")
            .WithSummary("Get a page of sales invoices, optionally filtered by search text");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetSalesInvoiceById")
            .WithSummary("Get a sales invoice by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateSalesInvoiceCommand>>()
            .WithName("CreateSalesInvoice")
            .WithSummary("Create a draft sales invoice (or post it directly — assigning a BIR-OR number and posting AR/Revenue/VAT — if quick-post is enabled)");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateSalesInvoice")
            .WithSummary("Update a draft sales invoice");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteSalesInvoice")
            .WithSummary("Delete a draft sales invoice");

        group.MapPost("/{id:guid}/submit", Submit)
            .WithName("SubmitSalesInvoice")
            .WithSummary("Submit a draft sales invoice for approval");

        group.MapPost("/{id:guid}/approve", Approve)
            .WithName("ApproveSalesInvoice")
            .WithSummary("Approve a pending sales invoice — assigns a BIR-OR number and posts AR/Revenue/VAT");

        group.MapPost("/{id:guid}/reject", Reject)
            .WithName("RejectSalesInvoice")
            .WithSummary("Reject a pending sales invoice back to draft");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .WithName("CancelSalesInvoice")
            .WithSummary("Cancel a draft or pending-approval sales invoice directly");

        group.MapPost("/{id:guid}/request-cancellation", RequestCancellation)
            .WithName("RequestSalesInvoiceCancellation")
            .WithSummary("Request cancellation of a posted sales invoice");

        group.MapPost("/{id:guid}/approve-cancellation", ApproveCancellation)
            .WithName("ApproveSalesInvoiceCancellation")
            .WithSummary("Approve a cancellation request — reverses the posted GL journal");

        group.MapPost("/{id:guid}/reject-cancellation", RejectCancellation)
            .WithName("RejectSalesInvoiceCancellation")
            .WithSummary("Reject a cancellation request — the document stands as posted");

        group.MapPost("/{id:guid}/record-print", RecordPrint)
            .WithName("RecordSalesInvoicePrint")
            .WithSummary("Record that the BIR receipt for this invoice was printed — increments the audit print counter and reports whether this is a reprint");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSalesInvoicesQuery, Result<List<SalesInvoiceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSalesInvoicesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllPaged(
        int? page,
        int? pageSize,
        string? search,
        IQueryHandler<GetAllSalesInvoicesPagedQuery, Result<PagedResult<SalesInvoiceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSalesInvoicesPagedQuery(page ?? 1, pageSize ?? 20, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetSalesInvoiceQuery, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesInvoiceQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateSalesInvoiceCommand command,
        ICommandHandler<CreateSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetSalesInvoiceById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateSalesInvoiceRequest request,
        IValidator<UpdateSalesInvoiceCommand> validator,
        ICommandHandler<UpdateSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSalesInvoiceCommand(
            id, request.BranchId, request.CustomerId, request.DeliveryOrderId, request.InvoiceDate, request.DueDate,
            request.Remarks, request.DiscountPct, request.CostCenterId, request.UpdatedBy, request.Lines);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteSalesInvoiceCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteSalesInvoiceCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(
        Guid id,
        SubmitSalesInvoiceRequest request,
        IValidator<SubmitSalesInvoiceCommand> validator,
        ICommandHandler<SubmitSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSalesInvoiceCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(
        Guid id,
        DecideSalesInvoiceRequest request,
        IValidator<ApproveSalesInvoiceCommand> validator,
        ICommandHandler<ApproveSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesInvoiceCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(
        Guid id,
        DecideSalesInvoiceRequiredCommentRequest request,
        IValidator<RejectSalesInvoiceCommand> validator,
        ICommandHandler<RejectSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesInvoiceCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Cancel(
        Guid id,
        CancelSalesInvoiceRequest request,
        IValidator<CancelSalesInvoiceCommand> validator,
        ICommandHandler<CancelSalesInvoiceCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelSalesInvoiceCommand(id, request.CancelledBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RequestCancellation(
        Guid id,
        RequestSalesInvoiceCancellationRequest request,
        IValidator<RequestSalesInvoiceCancellationCommand> validator,
        ICommandHandler<RequestSalesInvoiceCancellationCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RequestSalesInvoiceCancellationCommand(id, request.RequestedBy, request.Reason);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> ApproveCancellation(
        Guid id,
        DecideSalesInvoiceRequest request,
        IValidator<ApproveSalesInvoiceCancellationCommand> validator,
        ICommandHandler<ApproveSalesInvoiceCancellationCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSalesInvoiceCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RejectCancellation(
        Guid id,
        DecideSalesInvoiceRequiredCommentRequest request,
        IValidator<RejectSalesInvoiceCancellationCommand> validator,
        ICommandHandler<RejectSalesInvoiceCancellationCommand, Result<SalesInvoiceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectSalesInvoiceCancellationCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RecordPrint(
        Guid id,
        RecordSalesInvoicePrintRequest request,
        ICommandHandler<RecordSalesInvoicePrintCommand, Result<RecordSalesInvoicePrintResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RecordSalesInvoicePrintCommand(id, request.PrintedBy), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateSalesInvoiceRequest(
    string BranchId,
    Guid CustomerId,
    Guid? DeliveryOrderId,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    decimal? DiscountPct,
    Guid? CostCenterId,
    string? UpdatedBy,
    List<SalesInvoiceLineInput> Lines);

// SalesInvoiceLineInput is defined in CreateSalesInvoiceCommand.cs and reused as-is for Update —
// same convention as SalesOrderLineInput/DeliveryOrderLineInput.

public sealed record SubmitSalesInvoiceRequest(string RequestedBy);
public sealed record DecideSalesInvoiceRequest(string ApproverUserId, string? Comments);
public sealed record DecideSalesInvoiceRequiredCommentRequest(string ApproverUserId, string Comments);
public sealed record CancelSalesInvoiceRequest(string CancelledBy, string Reason);
public sealed record RequestSalesInvoiceCancellationRequest(string RequestedBy, string Reason);
public sealed record RecordSalesInvoicePrintRequest(string? PrintedBy);
