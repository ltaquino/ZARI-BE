namespace ZARI.Application.Features.Loan.Reports.CisaCreditData.Shared;

/// <summary>
/// CDA MC 2019-01's exposure-profile aging buckets — finer-grained than the Delinquency report's
/// 5-bucket scheme (current/1-30/31-60/61-90/90+), since CIC's Basic Credit Data spec calls for
/// 1-30/31-60/61-90/91-120/121-150/151-180/over-180. Kept as its own bucketing function rather than
/// generalizing GetLoanDelinquencyReportQueryHandler's BucketOf, since the two serve different
/// regulators with different prescribed buckets — coincidentally similar shape, not the same rule.
/// </summary>
internal static class CisaAgingBucket
{
    public static string Of(int daysOverdue) => daysOverdue switch
    {
        <= 0 => "CURRENT",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        <= 120 => "91-120",
        <= 150 => "121-150",
        <= 180 => "151-180",
        _ => "OVER-180",
    };
}
