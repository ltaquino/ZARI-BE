using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PayrollRuns.Approve;
using ZARI.Application.Features.Hr.PayrollRuns.Compute;
using ZARI.Application.Features.Hr.PayrollRuns.Create;
using ZARI.Application.Features.Hr.PayrollRuns.Delete;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.GetAll;
using ZARI.Application.Features.Hr.PayrollRuns.Reject;
using ZARI.Application.Features.Hr.PayrollRuns.Submit;
using ZARI.Application.Features.Hr.PayrollRuns.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PayrollRunEndpoints
{
    public static void MapPayrollRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll-runs")
            .WithTags("PayrollRuns")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllPayrollRuns").WithSummary("Get all payroll runs");
        group.MapGet("/{id:guid}", GetById).WithName("GetPayrollRunById").WithSummary("Get a payroll run by ID, including its lines");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreatePayrollRunCommand>>().WithName("CreatePayrollRun").WithSummary("Create a new draft payroll run");
        group.MapPut("/{id:guid}", Update).WithName("UpdatePayrollRun").WithSummary("Update an existing draft payroll run's period/pay date");
        group.MapDelete("/{id:guid}", Delete).WithName("DeletePayrollRun").WithSummary("Delete a draft payroll run");
        group.MapPost("/{id:guid}/compute", Compute).WithName("ComputePayrollRun").WithSummary("(Re)generate this draft payroll run's lines");
        group.MapPost("/{id:guid}/submit", Submit).WithName("SubmitPayrollRun").WithSummary("Submit a draft payroll run for approval");
        group.MapPost("/{id:guid}/approve", Approve).WithName("ApprovePayrollRun").WithSummary("Approve a pending payroll run — posts its aggregated GL journal");
        group.MapPost("/{id:guid}/reject", Reject).WithName("RejectPayrollRun").WithSummary("Reject a pending payroll run back to draft");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllPayrollRunsQuery, Result<List<PayrollRunSummaryResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPayrollRunsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetPayrollRunQuery, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPayrollRunQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreatePayrollRunCommand command, ICommandHandler<CreatePayrollRunCommand, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetPayrollRunById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdatePayrollRunRequest request, IValidator<UpdatePayrollRunCommand> validator, ICommandHandler<UpdatePayrollRunCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdatePayrollRunCommand(id, request.PayPeriodStart, request.PayPeriodEnd, request.PayDate);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeletePayrollRunCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePayrollRunCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Compute(Guid id, ICommandHandler<ComputePayrollRunCommand, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ComputePayrollRunCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Submit(Guid id, SubmitPayrollRunRequest request, IValidator<SubmitPayrollRunCommand> validator, ICommandHandler<SubmitPayrollRunCommand, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new SubmitPayrollRunCommand(id, request.RequestedBy);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Approve(Guid id, DecidePayrollRunRequest request, IValidator<ApprovePayrollRunCommand> validator, ICommandHandler<ApprovePayrollRunCommand, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new ApprovePayrollRunCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reject(Guid id, DecidePayrollRunRequiredCommentRequest request, IValidator<RejectPayrollRunCommand> validator, ICommandHandler<RejectPayrollRunCommand, Result<PayrollRunResponse>> handler, CancellationToken cancellationToken)
    {
        var command = new RejectPayrollRunCommand(id, request.ApproverUserId, request.Comments);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdatePayrollRunRequest(DateTimeOffset PayPeriodStart, DateTimeOffset PayPeriodEnd, DateTimeOffset PayDate);
public sealed record SubmitPayrollRunRequest(string RequestedBy);
public sealed record DecidePayrollRunRequest(string ApproverUserId, string? Comments);
public sealed record DecidePayrollRunRequiredCommentRequest(string ApproverUserId, string Comments);
