using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Read-only on purpose — see <see cref="StockLedgerEndpoints"/>'s doc comment for the full
/// rationale. Receive/Issue/ReverseIssue/ReverseReceive are internal composition primitives
/// called in-process from GoodsReceipt/GoodsIssue/etc.'s own Approve handlers; the raw HTTP
/// endpoints had no permission check and no FE call site, so they were removed rather than
/// permission-gated.
/// </summary>
public static class SerialNumberEndpoints
{
    public static void MapSerialNumberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/serial-numbers")
            .WithTags("SerialNumbers")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSerialNumbers")
            .WithSummary("Get all serial numbers");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSerialNumbersQuery, Result<List<SerialNumberResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSerialNumbersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
