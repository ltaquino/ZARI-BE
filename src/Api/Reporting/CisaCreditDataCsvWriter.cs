using System.Globalization;
using System.Text;
using ZARI.Application.Features.Loan.Reports.CisaCreditData;

namespace ZARI.Api.Reporting;

/// <summary>
/// Flattens the CISA Basic Credit Data export into an RFC4180-ish CSV — the actual CIC submission
/// file format isn't published anywhere findable (see GetCisaCreditDataExportQueryHandler's doc
/// comment), so this is a compliance-officer-readable extract to hand-map into whatever CIC
/// actually requires, not a submission-ready file in CIC's own schema.
/// </summary>
public static class CisaCreditDataCsvWriter
{
    private static readonly string[] Headers =
    [
        "CustomerId", "MemberNo", "CustomerName", "TIN", "SSS/GSIS No.", "DateOfBirth", "Sex", "CivilStatus", "DependentsCount",
        "Employer", "EmployerPosition", "NetIncomeLastYear", "ResidenceSince", "PriorResidenceHistory", "EmploymentSince",
        "PriorEmploymentHistory", "HousingStatus", "OwnsVehicle", "BankAccountInfo", "OtherAssetsNotes",
        "DataSharingConsent", "DataSharingConsentDate",
        "LoanAccountId", "LoanAcctNo", "BranchId", "ObligationType", "Secured", "AccountOpeningDate", "PrincipalAmount",
        "OutstandingPrincipalBalance", "PaymentMode", "RemainingInstallments", "AccountStatus", "LastActivityDate",
        "ArrearsEntryDate", "DaysOverdue", "PastDueAgingBucket", "WasRestructured", "WriteOffDate", "IsDisputed", "DisputeNotes",
        "NegativeCreditRecordsSummary"
    ];

    public static byte[] Write(List<CisaCreditDataRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers.Select(Escape)));

        foreach (var r in rows)
        {
            var fields = new object?[]
            {
                r.CustomerId, r.MemberNo, r.CustomerName, r.Tin, r.SssOrGsisNo, FormatDate(r.DateOfBirth), r.Sex, r.CivilStatus, r.DependentsCount,
                r.Employer, r.EmployerPosition, r.NetIncomeLastYear, FormatDate(r.ResidenceSince), r.PriorResidenceHistory, FormatDate(r.EmploymentSince),
                r.PriorEmploymentHistory, r.HousingStatus, r.OwnsVehicle, r.BankAccountInfo, r.OtherAssetsNotes,
                r.DataSharingConsent, FormatDate(r.DataSharingConsentDate),
                r.LoanAccountId, r.LoanAcctNo, r.BranchId, r.ObligationType, r.Secured, FormatDate(r.AccountOpeningDate), r.PrincipalAmount,
                r.OutstandingPrincipalBalance, r.PaymentMode, r.RemainingInstallments, r.AccountStatus, FormatDate(r.LastActivityDate),
                FormatDate(r.ArrearsEntryDate), r.DaysOverdue, r.PastDueAgingBucket, r.WasRestructured, FormatDate(r.WriteOffDate), r.IsDisputed, r.DisputeNotes,
                r.NegativeCreditRecordsSummary
            };
            sb.AppendLine(string.Join(",", fields.Select(f => Escape(FormatValue(f)))));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string? FormatDate(DateTimeOffset? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        bool b => b ? "Y" : "N",
        decimal dec => dec.ToString("0.00", CultureInfo.InvariantCulture),
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
