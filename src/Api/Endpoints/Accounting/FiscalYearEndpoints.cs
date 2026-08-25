using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.FiscalYears.Create;
using ZARI.Application.Features.Accounting.FiscalYears.Delete;
using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Application.Features.Accounting.FiscalYears.GetAll;
using ZARI.Application.Features.Accounting.FiscalYears.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class FiscalYearEndpoints
{
    public static void MapFiscalYearEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fiscal-years")
            .WithTags("FiscalYears")
            .WithGroupName("Accounting")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllFiscalYears")
            .WithSummary("Get all fiscal years");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetFiscalYearById")
            .WithSummary("Get a fiscal year by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateFiscalYearCommand>>()
            .WithName("CreateFiscalYear")
            .WithSummary("Create a new fiscal year");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateFiscalYear")
            .WithSummary("Update an existing fiscal year");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteFiscalYear")
            .WithSummary("Delete a fiscal year");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllFiscalYearsQuery, Result<List<FiscalYearResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllFiscalYearsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetFiscalYearQuery, Result<FiscalYearResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetFiscalYearQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateFiscalYearCommand command,
        ICommandHandler<CreateFiscalYearCommand, Result<FiscalYearResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetFiscalYearById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateFiscalYearRequest request,
        IValidator<UpdateFiscalYearCommand> validator,
        ICommandHandler<UpdateFiscalYearCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFiscalYearCommand(id, request.YearName, request.StartDate, request.EndDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteFiscalYearCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteFiscalYearCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateFiscalYearRequest(string YearName, DateTimeOffset StartDate, DateTimeOffset EndDate, string Status);
