using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.CreateAdjustment;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.GetAll;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// No Update/Delete endpoints — the ledger is append-only by design (HrModuleContext.md §3.8), so
/// only Get/GetAll (read) and the CreateAdjustment command (post a new row) are exposed.
/// </summary>
public static class LeaveBalanceEntryEndpoints
{
    public static void MapLeaveBalanceEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leave-balance-entries")
            .WithTags("LeaveBalanceEntries")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllLeaveBalanceEntries").WithSummary("Get all leave balance ledger entries");
        group.MapGet("/{id:guid}", GetById).WithName("GetLeaveBalanceEntryById").WithSummary("Get a leave balance ledger entry by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateLeaveBalanceAdjustmentCommand>>().WithName("CreateLeaveBalanceAdjustment").WithSummary("Post an ACCRUAL/CARRYOVER/ADJUSTMENT ledger entry");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllLeaveBalanceEntriesQuery, Result<List<LeaveBalanceEntryResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLeaveBalanceEntriesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetLeaveBalanceEntryQuery, Result<LeaveBalanceEntryResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLeaveBalanceEntryQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateLeaveBalanceAdjustmentCommand command, ICommandHandler<CreateLeaveBalanceAdjustmentCommand, Result<LeaveBalanceEntryResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetLeaveBalanceEntryById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }
}
