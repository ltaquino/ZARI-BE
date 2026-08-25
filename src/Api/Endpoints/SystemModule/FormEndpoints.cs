using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Forms.GetAll;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class FormEndpoints
{
    public static void MapFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forms")
            .WithTags("Forms")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllForms")
            .WithSummary("Get the forms catalog that Role and per-user permissions are granted against");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllFormsQuery, Result<List<FormResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllFormsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
