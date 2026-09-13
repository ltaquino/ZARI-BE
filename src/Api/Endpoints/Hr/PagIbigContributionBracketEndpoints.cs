using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Create;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Delete;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.GetAll;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PagIbigContributionBracketEndpoints
{
    public static void MapPagIbigContributionBracketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pagibig-contribution-brackets")
            .WithTags("PagIbigContributionBrackets")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllPagIbigContributionBrackets").WithSummary("Get all Pag-IBIG contribution brackets");
        group.MapGet("/{id:guid}", GetById).WithName("GetPagIbigContributionBracketById").WithSummary("Get a Pag-IBIG contribution bracket by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreatePagIbigContributionBracketCommand>>().WithName("CreatePagIbigContributionBracket").WithSummary("Create a new Pag-IBIG contribution bracket");
        group.MapPut("/{id:guid}", Update).WithName("UpdatePagIbigContributionBracket").WithSummary("Update an existing Pag-IBIG contribution bracket");
        group.MapDelete("/{id:guid}", Delete).WithName("DeletePagIbigContributionBracket").WithSummary("Delete a Pag-IBIG contribution bracket");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllPagIbigContributionBracketsQuery, Result<List<PagIbigContributionBracketResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPagIbigContributionBracketsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetPagIbigContributionBracketQuery, Result<PagIbigContributionBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPagIbigContributionBracketQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreatePagIbigContributionBracketCommand command, ICommandHandler<CreatePagIbigContributionBracketCommand, Result<PagIbigContributionBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetPagIbigContributionBracketById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdatePagIbigContributionBracketRequest request, IValidator<UpdatePagIbigContributionBracketCommand> validator, ICommandHandler<UpdatePagIbigContributionBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdatePagIbigContributionBracketCommand(id, request.SalaryFrom, request.SalaryTo, request.EmployeeShare, request.EmployerShare, request.EffectiveDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeletePagIbigContributionBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePagIbigContributionBracketCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdatePagIbigContributionBracketRequest(decimal SalaryFrom, decimal SalaryTo, decimal EmployeeShare, decimal EmployerShare, DateTimeOffset EffectiveDate, string Status);
