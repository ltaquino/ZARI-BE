using System.Text.Json;
using FluentValidation;
using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets.Get;
using ZARI.Application.Features.Reporting.Datasets.Values;
using ZARI.Application.Features.Reporting.ReportTemplates.Create;
using ZARI.Application.Features.Reporting.ReportTemplates.Delete;
using ZARI.Application.Features.Reporting.ReportTemplates.Get;
using ZARI.Application.Features.Reporting.ReportTemplates.GetAll;
using ZARI.Application.Features.Reporting.ReportTemplates.Run;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Application.Features.Reporting.ReportTemplates.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reporting")
            .WithTags("Reporting")
            .WithGroupName("Reporting")
            .RequireAuthorization();

        group.MapGet("/datasets", GetDatasets)
            .WithName("GetReportDatasets")
            .WithSummary("Catalog of datasets the current user may build report templates against");

        group.MapGet("/datasets/{datasetKey}/fields/{fieldKey}/values", GetFieldValues)
            .WithName("GetReportFieldValues")
            .WithSummary("Distinct known values for one dataset field, for a searchable filter-value dropdown");

        group.MapGet("/templates", GetAllTemplates)
            .WithName("GetAllReportTemplates")
            .WithSummary("Get all report templates visible to the current user (own + shared)");

        group.MapGet("/templates/{id:guid}", GetTemplateById)
            .WithName("GetReportTemplateById")
            .WithSummary("Get one report template's full definition, for the designer's Edit mode");

        group.MapPost("/templates", Create)
            .AddEndpointFilter<ValidationFilter<CreateReportTemplateCommand>>()
            .WithName("CreateReportTemplate")
            .WithSummary("Create a new report template");

        group.MapPut("/templates/{id:guid}", Update)
            .WithName("UpdateReportTemplate")
            .WithSummary("Update an existing report template");

        group.MapDelete("/templates/{id:guid}", Delete)
            .WithName("DeleteReportTemplate")
            .WithSummary("Delete a report template");

        // Runtime filter overrides (for whichever of the template's filters were saved with
        // PromptAtRuntime=true) are passed as `overridesJson` — a single query-string param holding
        // a JSON-encoded array of {fieldKey,value,value2} objects — because minimal-API cannot bind
        // an arbitrary-length structured list from the query string any other way.
        group.MapGet("/templates/{id:guid}/run", Run)
            .WithName("RunReportTemplate")
            .WithSummary("Run a saved report template against live data");

        group.MapGet("/templates/{id:guid}/run/pdf", RunPdf)
            .WithName("RunReportTemplatePdf")
            .WithSummary("Run a saved report template and return the result as a PDF");
    }

    private static List<RunReportTemplateFilterOverride> ParseOverrides(string? overridesJson)
    {
        if (string.IsNullOrWhiteSpace(overridesJson)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<RunReportTemplateFilterOverride>>(
                overridesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static async Task<IResult> GetDatasets(
        IQueryHandler<GetReportDatasetsQuery, Result<List<ReportDatasetResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReportDatasetsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetFieldValues(
        string datasetKey,
        string fieldKey,
        string? search,
        IQueryHandler<GetReportFieldValuesQuery, Result<List<string>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReportFieldValuesQuery(datasetKey, fieldKey, search), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetAllTemplates(
        IQueryHandler<GetAllReportTemplatesQuery, Result<List<ReportTemplateSummaryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllReportTemplatesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetTemplateById(
        Guid id,
        IQueryHandler<GetReportTemplateQuery, Result<ReportTemplateDetailResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReportTemplateQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateReportTemplateCommand command,
        ICommandHandler<CreateReportTemplateCommand, Result<ReportTemplateDetailResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetReportTemplateById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateReportTemplateRequest request,
        IValidator<UpdateReportTemplateCommand> validator,
        ICommandHandler<UpdateReportTemplateCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReportTemplateCommand(
            id,
            request.Name,
            request.Description,
            request.DatasetKey,
            request.PaperSize,
            request.Orientation,
            request.HeaderText,
            request.FooterText,
            request.ShowColumnTotals,
            request.IsShared,
            request.Columns,
            request.Filters,
            request.Sort,
            request.GroupByFieldKeys);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteReportTemplateCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteReportTemplateCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Run(
        Guid id,
        string? overridesJson,
        IQueryHandler<RunReportTemplateQuery, Result<RunReportTemplateResponse>> handler,
        CancellationToken cancellationToken)
    {
        var overrides = ParseOverrides(overridesJson);
        var result = await handler.HandleAsync(new RunReportTemplateQuery(id, overrides), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> RunPdf(
        Guid id,
        string? overridesJson,
        IQueryHandler<RunReportTemplateQuery, Result<RunReportTemplateResponse>> handler,
        CancellationToken cancellationToken)
    {
        var overrides = ParseOverrides(overridesJson);
        var result = await handler.HandleAsync(new RunReportTemplateQuery(id, overrides), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new GenericTableReportDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", $"{result.Value!.TemplateName}.pdf");
    }
}

public sealed record UpdateReportTemplateRequest(
    string Name,
    string? Description,
    string DatasetKey,
    string PaperSize,
    string Orientation,
    string? HeaderText,
    string? FooterText,
    bool ShowColumnTotals,
    bool IsShared,
    List<ReportTemplateColumn> Columns,
    List<ReportTemplateFilter> Filters,
    ReportTemplateSort? Sort,
    List<string> GroupByFieldKeys);
