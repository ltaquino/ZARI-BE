using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.ExchangeRates.Create;
using ZARI.Application.Features.Accounting.ExchangeRates.Delete;
using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Application.Features.Accounting.ExchangeRates.GetAll;
using ZARI.Application.Features.Accounting.ExchangeRates.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ExchangeRateEndpoints
{
    public static void MapExchangeRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exchange-rates")
            .WithTags("ExchangeRates")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllExchangeRates")
            .WithSummary("Get all exchange rates");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetExchangeRateById")
            .WithSummary("Get an exchange rate by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateExchangeRateCommand>>()
            .WithName("CreateExchangeRate")
            .WithSummary("Create a new exchange rate");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateExchangeRate")
            .WithSummary("Update an existing exchange rate");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteExchangeRate")
            .WithSummary("Delete an exchange rate");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllExchangeRatesQuery, Result<List<ExchangeRateResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllExchangeRatesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetExchangeRateQuery, Result<ExchangeRateResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExchangeRateQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateExchangeRateCommand command,
        ICommandHandler<CreateExchangeRateCommand, Result<ExchangeRateResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetExchangeRateById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateExchangeRateRequest request,
        IValidator<UpdateExchangeRateCommand> validator,
        ICommandHandler<UpdateExchangeRateCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateExchangeRateCommand(id, request.CurrencyId, request.RateDate, request.RateToBase);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteExchangeRateCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteExchangeRateCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateExchangeRateRequest(string CurrencyId, DateTimeOffset RateDate, decimal RateToBase);
