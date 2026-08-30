using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosClosing.GetAllZReadings;
using ZARI.Application.Features.Sales.PosClosing.RunXReading;
using ZARI.Application.Features.Sales.PosClosing.RunZReading;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PosClosingEndpoints
{
    public static void MapPosClosingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos-closing")
            .WithTags("PosClosing")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/x-reading", GetXReading)
            .WithName("RunXReading")
            .WithSummary("Read-only snapshot of sales since the last Z-Reading, up to now — can be run any number of times, never changes anything");

        group.MapPost("/z-reading", RunZReading)
            .WithName("RunZReading")
            .WithSummary("End-of-day BIR close — permanently closes every POSTED sales invoice since the last Z-Reading and increments the branch's Z-Counter");

        group.MapGet("/z-readings", GetAllZReadings)
            .WithName("GetAllZReadings")
            .WithSummary("History of past Z-Readings for a branch");
    }

    private static async Task<IResult> GetXReading(
        string branchId,
        IQueryHandler<RunXReadingQuery, Result<XReadingResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RunXReadingQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RunZReading(
        RunZReadingRequest request,
        ICommandHandler<RunZReadingCommand, Result<ZReadingResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RunZReadingCommand(request.BranchId, request.RunBy);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllZReadings(
        string branchId,
        IQueryHandler<GetAllZReadingsQuery, Result<List<ZReadingResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllZReadingsQuery(branchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record RunZReadingRequest(string BranchId, string RunBy);
