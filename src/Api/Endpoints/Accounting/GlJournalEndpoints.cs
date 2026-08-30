using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

/// <summary>
/// Read-only on purpose. Posting/reversing a GL journal is never a direct user action — every
/// module posts through its own Approve handler (which already enforces that module's own
/// branch/permission checks) via in-process <c>ICommandHandler&lt;PostGlJournalCommand,...&gt;</c>/
/// <c>ICommandHandler&lt;ReverseGlJournalsCommand,...&gt;</c> injection, never over HTTP. Both
/// commands used to also be mapped as raw <c>POST /api/gl-journals/post</c>/<c>/reverse</c>
/// endpoints with no permission check of their own (neither command handler takes an
/// IPermissionService) — since nothing legitimate ever called them (confirmed: no FE call site
/// exists), that was a live authorization bypass letting any authenticated user post or reverse
/// an arbitrary GL journal for any branch/source document. Removed rather than permission-gated,
/// since a raw "post any journal" capability isn't a real feature this app should expose at all.
/// </summary>
public static class GlJournalEndpoints
{
    public static void MapGlJournalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gl-journals")
            .WithTags("GlJournals")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllGlJournals")
            .WithSummary("Get all GL journals");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGlJournalsQuery, Result<List<GlJournalResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGlJournalsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
