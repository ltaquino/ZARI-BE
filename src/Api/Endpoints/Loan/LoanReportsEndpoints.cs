using QuestPDF.Fluent;
using ZARI.Api.Extensions;
using ZARI.Api.Reporting;
using ZARI.Application.Abstractions.Messaging;
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
}
