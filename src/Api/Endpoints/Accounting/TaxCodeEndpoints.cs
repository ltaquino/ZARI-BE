using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.TaxCodes.Create;
using ZARI.Application.Features.Accounting.TaxCodes.Delete;
using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Application.Features.Accounting.TaxCodes.GetAll;
using ZARI.Application.Features.Accounting.TaxCodes.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class TaxCodeEndpoints
{
    public static void MapTaxCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tax-codes")
            .WithTags("TaxCodes")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllTaxCodes")
            .WithSummary("Get all tax codes");

        group.MapGet("/{code}", GetById)
            .WithName("GetTaxCodeById")
            .WithSummary("Get a tax code by code");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateTaxCodeCommand>>()
            .WithName("CreateTaxCode")
            .WithSummary("Create a new tax code");

        group.MapPut("/{code}", Update)
            .WithName("UpdateTaxCode")
            .WithSummary("Update an existing tax code");

        group.MapDelete("/{code}", Delete)
            .WithName("DeleteTaxCode")
            .WithSummary("Delete a tax code");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllTaxCodesQuery, Result<List<TaxCodeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllTaxCodesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        string code,
        IQueryHandler<GetTaxCodeQuery, Result<TaxCodeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTaxCodeQuery(code), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateTaxCodeCommand command,
        ICommandHandler<CreateTaxCodeCommand, Result<TaxCodeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetTaxCodeById", new { code = result.Value!.Code })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        string code,
        UpdateTaxCodeRequest request,
        IValidator<UpdateTaxCodeCommand> validator,
        ICommandHandler<UpdateTaxCodeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaxCodeCommand(code, request.Name, request.Rate, request.TaxType, request.GlAccountId);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        string code,
        ICommandHandler<DeleteTaxCodeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteTaxCodeCommand(code), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateTaxCodeRequest(string? Name, decimal Rate, string TaxType, Guid? GlAccountId);
