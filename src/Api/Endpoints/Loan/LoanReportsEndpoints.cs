using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality;
using ZARI.Application.Features.Loan.Reports.CicCsdf;
using ZARI.Application.Features.Loan.Reports.CisaCreditData;
using ZARI.Application.Features.Loan.Reports.LoanDelinquency;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanReportsEndpoints
{
    public static void MapLoanReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan/reports").WithTags("LoanReports").WithGroupName("Loan").RequireAuthorization();

        group.MapGet("/delinquency", GetDelinquency)
            .WithName("GetLoanDelinquencyReport")
            .WithSummary("Loan Delinquency / Past-Due report — outstanding installments on every active loan account, grouped by member and how overdue they are");

        group.MapGet("/delinquency/pdf", GetDelinquencyPdf)
            .WithName("GetLoanDelinquencyReportPdf")
            .WithSummary("Loan Delinquency / Past-Due report as a PDF");

        group.MapGet("/cisa-credit-data", GetCisaCreditData)
            .WithName("GetCisaCreditDataExport")
            .WithSummary("RA 9510 (CISA) / CDA MC 2019-01 Basic Credit Data extract — one row per loan account, borrower profile flattened in");

        group.MapGet("/cisa-credit-data/csv", GetCisaCreditDataCsv)
            .WithName("GetCisaCreditDataExportCsv")
            .WithSummary("Basic Credit Data extract as a CSV a compliance officer can hand-map into the CIC's own submission format");

        group.MapGet("/cda-portfolio-quality", GetCdaPortfolioQuality)
            .WithName("GetCdaPortfolioQualityReport")
            .WithSummary("CDA MC 02-04 Loan Portfolio Quality — Portfolio at Risk and Allowance for Probable Losses per loan account");

        group.MapGet("/cda-portfolio-quality/csv", GetCdaPortfolioQualityCsv)
            .WithName("GetCdaPortfolioQualityReportCsv")
            .WithSummary("CDA Loan Portfolio Quality report as a CSV");

        group.MapGet("/cic-csdf", GetCicCsdf)
            .WithName("GetCicCsdfExport")
            .WithSummary("CIC CSDF v1.4 submission data (RA 9510) as JSON — the raw ID/CI field arrays, for inspection before download");

        group.MapGet("/cic-csdf/file", GetCicCsdfFile)
            .WithName("GetCicCsdfExportFile")
            .WithSummary("The real CIC CSDF v1.4 submission file — pipe-delimited, UTF-8 without BOM, named [ProviderCode]_CSDF_[timestamp].txt per CIC's own spec. Still needs to be manually zipped, PGP-encrypted, and uploaded via FTPS — see ZARI-FE/frs/loan-cic/LoanCicContext.md §1.");
    }

    private static async Task<IResult> GetDelinquency(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetLoanDelinquencyReportQuery, Result<LoanDelinquencyReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery(branchId, customerId, asOfDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetDelinquencyPdf(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetLoanDelinquencyReportQuery, Result<LoanDelinquencyReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanDelinquencyReportQuery(branchId, customerId, asOfDate), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = new LoanDelinquencyDocument(result.Value!).GeneratePdf();
        return Results.File(bytes, "application/pdf", "loan-delinquency-report.pdf");
    }

    private static async Task<IResult> GetCisaCreditData(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetCisaCreditDataExportQuery, Result<List<CisaCreditDataRow>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(branchId, customerId, asOfDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCisaCreditDataCsv(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetCisaCreditDataExportQuery, Result<List<CisaCreditDataRow>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCisaCreditDataExportQuery(branchId, customerId, asOfDate), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = CisaCreditDataCsvWriter.Write(result.Value!);
        return Results.File(bytes, "text/csv", "cisa-basic-credit-data.csv");
    }

    private static async Task<IResult> GetCdaPortfolioQuality(
        string? branchId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetCdaPortfolioQualityReportQuery, Result<CdaPortfolioQualityReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(branchId, asOfDate), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCdaPortfolioQualityCsv(
        string? branchId,
        DateTimeOffset? asOfDate,
        IQueryHandler<GetCdaPortfolioQualityReportQuery, Result<CdaPortfolioQualityReportResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCdaPortfolioQualityReportQuery(branchId, asOfDate), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = CdaPortfolioQualityCsvWriter.Write(result.Value!);
        return Results.File(bytes, "text/csv", "cda-portfolio-quality.csv");
    }

    private static async Task<IResult> GetCicCsdf(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IConfiguration configuration,
        IQueryHandler<GetCicCsdfExportQuery, Result<CicCsdfExport>> handler,
        CancellationToken cancellationToken)
    {
        var providerCode = configuration["Cic:ProviderCode"];
        var result = await handler.HandleAsync(new GetCicCsdfExportQuery(branchId, customerId, asOfDate, providerCode), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCicCsdfFile(
        string? branchId,
        Guid? customerId,
        DateTimeOffset? asOfDate,
        IConfiguration configuration,
        IQueryHandler<GetCicCsdfExportQuery, Result<CicCsdfExport>> handler,
        CancellationToken cancellationToken)
    {
        var providerCode = configuration["Cic:ProviderCode"];
        var result = await handler.HandleAsync(new GetCicCsdfExportQuery(branchId, customerId, asOfDate, providerCode), cancellationToken);
        if (result.IsFailure) return result.ToProblemDetails();

        var bytes = CicCsdfFileWriter.Write(result.Value!);
        var fileName = CicCsdfFileWriter.FileName(result.Value!, DateTimeOffset.UtcNow);
        return Results.File(bytes, "text/plain", fileName);
    }
}
