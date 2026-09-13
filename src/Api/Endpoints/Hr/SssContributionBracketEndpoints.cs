using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.SssContributionBrackets.Create;
using ZARI.Application.Features.Hr.SssContributionBrackets.Delete;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Application.Features.Hr.SssContributionBrackets.GetAll;
using ZARI.Application.Features.Hr.SssContributionBrackets.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SssContributionBracketEndpoints
{
    public static void MapSssContributionBracketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sss-contribution-brackets")
            .WithTags("SssContributionBrackets")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllSssContributionBrackets").WithSummary("Get all SSS contribution brackets");
        group.MapGet("/{id:guid}", GetById).WithName("GetSssContributionBracketById").WithSummary("Get an SSS contribution bracket by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateSssContributionBracketCommand>>().WithName("CreateSssContributionBracket").WithSummary("Create a new SSS contribution bracket");
        group.MapPut("/{id:guid}", Update).WithName("UpdateSssContributionBracket").WithSummary("Update an existing SSS contribution bracket");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteSssContributionBracket").WithSummary("Delete an SSS contribution bracket");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllSssContributionBracketsQuery, Result<List<SssContributionBracketResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSssContributionBracketsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetSssContributionBracketQuery, Result<SssContributionBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSssContributionBracketQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateSssContributionBracketCommand command, ICommandHandler<CreateSssContributionBracketCommand, Result<SssContributionBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetSssContributionBracketById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateSssContributionBracketRequest request, IValidator<UpdateSssContributionBracketCommand> validator, ICommandHandler<UpdateSssContributionBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateSssContributionBracketCommand(id, request.SalaryFrom, request.SalaryTo, request.EmployeeShare, request.EmployerShare, request.EffectiveDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteSssContributionBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteSssContributionBracketCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateSssContributionBracketRequest(decimal SalaryFrom, decimal SalaryTo, decimal EmployeeShare, decimal EmployerShare, DateTimeOffset EffectiveDate, string Status);
