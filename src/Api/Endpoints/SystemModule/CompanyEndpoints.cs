using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Companies.Get;
using ZARI.Application.Features.SystemModule.Companies.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/company")
            .WithTags("Company")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", Get)
            .WithName("GetCompany")
            .WithSummary("Get the company settings record");

        group.MapPut("/", Update)
            .AddEndpointFilter<ValidationFilter<UpdateCompanyCommand>>()
            .WithName("UpdateCompany")
            .WithSummary("Update the company settings record");
    }

    private static async Task<IResult> Get(
        IQueryHandler<GetCompanyQuery, Result<CompanyResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCompanyQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        UpdateCompanyCommand command,
        ICommandHandler<UpdateCompanyCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
