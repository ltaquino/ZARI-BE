using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Create;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Delete;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.GetAll;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class WithholdingTaxBracketEndpoints
{
    public static void MapWithholdingTaxBracketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/withholding-tax-brackets")
            .WithTags("WithholdingTaxBrackets")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllWithholdingTaxBrackets").WithSummary("Get all withholding tax brackets");
        group.MapGet("/{id:guid}", GetById).WithName("GetWithholdingTaxBracketById").WithSummary("Get a withholding tax bracket by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateWithholdingTaxBracketCommand>>().WithName("CreateWithholdingTaxBracket").WithSummary("Create a new withholding tax bracket");
        group.MapPut("/{id:guid}", Update).WithName("UpdateWithholdingTaxBracket").WithSummary("Update an existing withholding tax bracket");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteWithholdingTaxBracket").WithSummary("Delete a withholding tax bracket");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllWithholdingTaxBracketsQuery, Result<List<WithholdingTaxBracketResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllWithholdingTaxBracketsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetWithholdingTaxBracketQuery, Result<WithholdingTaxBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWithholdingTaxBracketQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateWithholdingTaxBracketCommand command, ICommandHandler<CreateWithholdingTaxBracketCommand, Result<WithholdingTaxBracketResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetWithholdingTaxBracketById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateWithholdingTaxBracketRequest request, IValidator<UpdateWithholdingTaxBracketCommand> validator, ICommandHandler<UpdateWithholdingTaxBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateWithholdingTaxBracketCommand(id, request.SalaryFrom, request.SalaryTo, request.BaseTax, request.RateOverExcess, request.EffectiveDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteWithholdingTaxBracketCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteWithholdingTaxBracketCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateWithholdingTaxBracketRequest(decimal SalaryFrom, decimal SalaryTo, decimal BaseTax, decimal RateOverExcess, DateTimeOffset EffectiveDate, string Status);
