using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Create;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Delete;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Application.Features.Hr.GovernmentContributionRates.GetAll;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class GovernmentContributionRateEndpoints
{
    public static void MapGovernmentContributionRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/government-contribution-rates")
            .WithTags("GovernmentContributionRates")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllGovernmentContributionRates").WithSummary("Get all PhilHealth/Pag-IBIG contribution rates");
        group.MapGet("/{id:guid}", GetById).WithName("GetGovernmentContributionRateById").WithSummary("Get a government contribution rate by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateGovernmentContributionRateCommand>>().WithName("CreateGovernmentContributionRate").WithSummary("Create a new government contribution rate");
        group.MapPut("/{id:guid}", Update).WithName("UpdateGovernmentContributionRate").WithSummary("Update an existing government contribution rate");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteGovernmentContributionRate").WithSummary("Delete a government contribution rate");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllGovernmentContributionRatesQuery, Result<List<GovernmentContributionRateResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllGovernmentContributionRatesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetGovernmentContributionRateQuery, Result<GovernmentContributionRateResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGovernmentContributionRateQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateGovernmentContributionRateCommand command, ICommandHandler<CreateGovernmentContributionRateCommand, Result<GovernmentContributionRateResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetGovernmentContributionRateById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateGovernmentContributionRateRequest request, IValidator<UpdateGovernmentContributionRateCommand> validator, ICommandHandler<UpdateGovernmentContributionRateCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateGovernmentContributionRateCommand(id, request.ContributionType, request.SalaryFrom, request.SalaryTo, request.EmployeeSharePct, request.EmployerSharePct, request.MaxContribution, request.EffectiveDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteGovernmentContributionRateCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGovernmentContributionRateCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateGovernmentContributionRateRequest(string ContributionType, decimal SalaryFrom, decimal SalaryTo, decimal EmployeeSharePct, decimal EmployerSharePct, decimal? MaxContribution, DateTimeOffset EffectiveDate, string Status);
