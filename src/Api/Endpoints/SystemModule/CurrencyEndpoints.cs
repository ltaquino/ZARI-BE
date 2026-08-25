using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Currencies.Create;
using ZARI.Application.Features.SystemModule.Currencies.Delete;
using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Application.Features.SystemModule.Currencies.GetAll;
using ZARI.Application.Features.SystemModule.Currencies.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class CurrencyEndpoints
{
    public static void MapCurrencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/currencies")
            .WithTags("Currencies")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCurrencies")
            .WithSummary("Get all currencies");

        group.MapGet("/{id}", GetById)
            .WithName("GetCurrencyById")
            .WithSummary("Get a currency by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCurrencyCommand>>()
            .WithName("CreateCurrency")
            .WithSummary("Create a new currency");

        group.MapPut("/{id}", Update)
            .WithName("UpdateCurrency")
            .WithSummary("Update an existing currency");

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteCurrency")
            .WithSummary("Delete a currency");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCurrenciesQuery, Result<List<CurrencyResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCurrenciesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        string id,
        IQueryHandler<GetCurrencyQuery, Result<CurrencyResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCurrencyQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCurrencyCommand command,
        ICommandHandler<CreateCurrencyCommand, Result<CurrencyResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCurrencyById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        string id,
        UpdateCurrencyRequest request,
        IValidator<UpdateCurrencyCommand> validator,
        ICommandHandler<UpdateCurrencyCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCurrencyCommand(id, request.Code, request.Name, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        string id,
        ICommandHandler<DeleteCurrencyCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCurrencyCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateCurrencyRequest(string Code, string? Name, string Status);
