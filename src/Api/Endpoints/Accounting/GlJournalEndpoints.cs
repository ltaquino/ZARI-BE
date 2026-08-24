using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Accounting.GlJournals.Reverse;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

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

        group.MapPost("/post", Post)
            .AddEndpointFilter<ValidationFilter<PostGlJournalCommand>>()
            .WithName("PostGlJournal")
            .WithSummary("Post a balanced GL journal for a source document");

        group.MapPost("/reverse", Reverse)
            .AddEndpointFilter<ValidationFilter<ReverseGlJournalsCommand>>()
            .WithName("ReverseGlJournals")
            .WithSummary("Reverse every posted journal for a source document reference");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllGlJournalsQuery, Result<List<GlJournalResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGlJournalsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Post(
        PostGlJournalCommand command,
        ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Reverse(
        ReverseGlJournalsCommand command,
        ICommandHandler<ReverseGlJournalsCommand, Result<List<GlJournalResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
