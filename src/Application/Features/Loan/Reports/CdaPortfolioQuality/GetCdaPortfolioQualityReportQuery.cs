namespace ZARI.Application.Features.Loan.Reports.CdaPortfolioQuality;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Loan Portfolio Quality per CDA Memorandum Circular No. 02-04, Series of 2002 ("Standard Chart
/// of Accounts for Credit and Other Types of Cooperatives with Savings and Credit Services") — the
/// standing baseline circular (not the temporary COVID-19 relief measure in MC 2020-18). Implements
/// only the loan-specific "Portfolio Quality" piece of the COOP-PESOS performance-standards system
/// (Portfolio at Risk + Allowance for Probable Losses on Loans), not the full COOP-PESOS
/// governance/efficiency/stability rating — that covers far more than the Loan module (BOD
/// meetings, membership growth, deposit mobilization, etc.) and isn't loan-classification/
/// provisioning specific. This is a report only — no GL provisioning journal is posted; whether and
/// how to actually book an Allowance for Probable Losses is a real accounting-policy decision for
/// the cooperative (ZARI-FE/frs/loan/LoanModuleContext.md §6.4's caution applies here too).
/// AsOfDate defaults to today (server clock) when omitted.
/// </summary>
public sealed record GetCdaPortfolioQualityReportQuery(string? BranchId, DateTimeOffset? AsOfDate) : IQuery<Result<CdaPortfolioQualityReportResponse>>;

public sealed record CdaPortfolioQualityAccountRow(
    Guid LoanAccountId,
    string LoanAcctNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    string? MemberNo,
    decimal OutstandingPrincipalBalance,
    int DaysOverdue,
    // "CURRENT" | "PAST_DUE" | "IN_LITIGATION" | "RESTRUCTURED" — per MC 02-04's four Loans
    // Receivable sub-accounts (150-153).
    string Classification,
    // PAR = the ENTIRE outstanding balance once 1+ day overdue (MC 02-04's own definition — not
    // just the overdue installment amount, which is what the Delinquency report tracks instead).
    decimal ParAmount,
    // 0% / 35% / 100% purely off DaysOverdue, per the APLL(1-12mo)=35% / APLL(over 12mo)=100%
    // standards in the COOP-PESOS Portfolio Quality table — decoupled from Classification, since
    // the circular ties the percentage to age, not to which of the four sub-accounts it's booked in.
    decimal ApllRequiredPct,
    decimal ApllRequiredAmount);

public sealed record CdaPortfolioQualityReportResponse(
    List<CdaPortfolioQualityAccountRow> Accounts,
    decimal TotalLoansOutstanding,
    decimal TotalPar,
    decimal ParRatio,
    decimal TotalApllRequired,
    int CurrentCount,
    int PastDueCount,
    int InLitigationCount,
    int RestructuredCount);
