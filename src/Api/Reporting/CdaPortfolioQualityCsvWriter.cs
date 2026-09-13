using System.Globalization;
using System.Text;
using ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality;

namespace ZARI.Api.Reporting;

/// <summary>CSV export of the CDA MC 02-04 Loan Portfolio Quality report — see GetCdaPortfolioQualityReportQueryHandler's doc comment.</summary>
public static class CdaPortfolioQualityCsvWriter
{
    private static readonly string[] Headers =
    [
        "LoanAccountId", "LoanAcctNo", "BranchId", "CustomerId", "CustomerName", "MemberNo",
        "OutstandingPrincipalBalance", "DaysOverdue", "Classification", "ParAmount", "ApllRequiredPct", "ApllRequiredAmount"
    ];

    public static byte[] Write(CdaPortfolioQualityReportResponse report)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers.Select(Escape)));

        foreach (var r in report.Accounts)
        {
            var fields = new object?[]
            {
                r.LoanAccountId, r.LoanAcctNo, r.BranchId, r.CustomerId, r.CustomerName, r.MemberNo,
                r.OutstandingPrincipalBalance, r.DaysOverdue, r.Classification, r.ParAmount, r.ApllRequiredPct, r.ApllRequiredAmount
            };
            sb.AppendLine(string.Join(",", fields.Select(f => Escape(FormatValue(f)))));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        decimal dec => dec.ToString("0.0000", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
