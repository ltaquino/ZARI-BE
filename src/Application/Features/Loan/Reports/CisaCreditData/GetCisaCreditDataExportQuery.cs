namespace ZARI.Application.Features.Loan.Reports.CisaCreditData;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCisaCreditDataExportQuery(string? BranchId, Guid? CustomerId, DateTimeOffset? AsOfDate) : IQuery<Result<List<CisaCreditDataRow>>>;

/// <summary>
/// One row per LoanAccount, borrower profile fields flattened in — a CSV needs a flat table, and
/// negative credit records don't belong to any one account, so they're summarized into one column
/// rather than split into a second file. See CustomerCreditRecord's doc comment and
/// GetCisaCreditDataExportQueryHandler for what each derived field actually measures.
/// </summary>
public sealed record CisaCreditDataRow(
    // Borrower profile (RA 9510 / CDA MC 2019-01 "Basic Credit Data")
    Guid CustomerId,
    string? MemberNo,
    string CustomerName,
    string? Tin,
    string? SssOrGsisNo,
    DateTimeOffset? DateOfBirth,
    string? Sex,
    string? CivilStatus,
    int? DependentsCount,
    string? Employer,
    string? EmployerPosition,
    decimal? NetIncomeLastYear,
    DateTimeOffset? ResidenceSince,
    string? PriorResidenceHistory,
    DateTimeOffset? EmploymentSince,
    string? PriorEmploymentHistory,
    string? HousingStatus,
    bool OwnsVehicle,
    string? BankAccountInfo,
    string? OtherAssetsNotes,
    bool DataSharingConsent,
    DateTimeOffset? DataSharingConsentDate,
    // Exposure profile (per loan account)
    Guid LoanAccountId,
    string LoanAcctNo,
    string BranchId,
    string ObligationType,
    bool Secured,
    DateTimeOffset AccountOpeningDate,
    decimal PrincipalAmount,
    decimal OutstandingPrincipalBalance,
    string PaymentMode,
    int RemainingInstallments,
    string AccountStatus,
    DateTimeOffset? LastActivityDate,
    DateTimeOffset? ArrearsEntryDate,
    int DaysOverdue,
    string PastDueAgingBucket,
    bool WasRestructured,
    DateTimeOffset? WriteOffDate,
    bool IsDisputed,
    string? DisputeNotes,
    // Negative credit information (RA 9510's other required section), summarized per member
    string? NegativeCreditRecordsSummary);
