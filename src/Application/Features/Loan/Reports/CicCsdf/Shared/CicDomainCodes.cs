namespace ZARI.Application.Features.Loan.Reports.CicCsdf.Shared;

/// <summary>
/// Maps ZARI's own enum-shaped-string field values onto CIC's CSDF v1.4 domain codes (the value
/// tables in this folder's "Fields in Excel version 1.4_test.xlsx", sheets "ID - Individual" and
/// "CI - Installment Contract" — see ZARI-FE/frs/loan-cic/LoanCicContext.md §4). Every method here
/// is a small, fixed, CIC-defined lookup — not a cooperative-editable setting — same idiom as
/// CisaAgingBucket.Of. Values not sourced from the Excel (Good Type domain for LoanCollateral, the
/// full CountryDomain/PSIC/PSOC tables) are deliberately left unmapped rather than guessed at.
/// </summary>
internal static class CicDomainCodes
{
    /// GenderDomain: M=Male, F=Female.
    public static string? GenderCode(string? sex) => sex?.Trim().ToUpperInvariant() switch
    {
        "M" or "MALE" => "M",
        "F" or "FEMALE" => "F",
        _ => null,
    };

    /// CivilStatusDomain: 1=Single, 2=Married, 3=Divorced/Separated, 4=Widow(er). ZARI's
    /// Customer.CivilStatus is free text (no enforced value set) — matched case-insensitively
    /// against the values the FE is known to send plus common variants.
    public static string? CivilStatusCode(string? civilStatus) => civilStatus?.Trim().ToUpperInvariant() switch
    {
        "SINGLE" => "1",
        "MARRIED" => "2",
        "DIVORCED" or "SEPARATED" or "DIVORCED/SEPARATED" => "3",
        "WIDOW" or "WIDOWER" or "WIDOWED" => "4",
        _ => null,
    };

    /// HouseOwnerLesseeType: 1=Own, 2=Rent, 3=Lease, 4=Other. Matches Customer.AddressHouseOwnerOrLessee's
    /// own "OWN"/"RENT"/"LEASE"/"OTHER" convention.
    public static string? HouseOwnerLesseeCode(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "OWN" => "1",
        "RENT" => "2",
        "LEASE" => "3",
        "OTHER" => "4",
        _ => null,
    };

    /// ContactTypeDomain: only the two values ZARI actually has a source field for (Customer.Phone
    /// -> "3" Mobile phone, since ZARI's own Phone field is a single mobile-first contact number for
    /// a cooperative member, not a landline; Customer.Email -> "7" E-mail).
    public const string ContactTypePhone = "3";
    public const string ContactTypeEmail = "7";

    /// PaymentPeriodicityDomain, mapped from LoanAccount/LoanProduct.RepaymentFrequency's own
    /// "MONTHLY"/"SEMI_MONTHLY"/"WEEKLY" values (UpdateLoanRestructuringValidator.ValidFrequencies
    /// is the authoritative list of values this codebase actually uses).
    public static string? PaymentPeriodicityCode(string repaymentFrequency) => repaymentFrequency switch
    {
        "MONTHLY" => "M",
        "SEMI_MONTHLY" => "F", // closest CIC equivalent: "fortnight installments-15 days"
        "WEEKLY" => "W",
        _ => null,
    };

    /// PaymentMethodDomain, mapped from PaymentMethod.Code (AppDbSeeder.SeedPaymentMethodsAsync's
    /// own "CASH"/"CARD"/"GIFT_CHECK" codes).
    public static string PaymentMethodCode(string? paymentMethodCode) => paymentMethodCode switch
    {
        "CASH" => "CAS",
        "CARD" => "CCR",
        "GIFT_CHECK" => "CHQ",
        _ => "OTH",
    };

    /// RoleDomain: B=Borrower, C=Co-Borrower, G=Guarantor/Surety. Every LoanAccount's own borrower
    /// always reports as "B"; LoanCoMaker rows report as "G" (SIDC's co-makers function as
    /// guarantors — see LoanCicContext.md §6 #4 for the still-open "do they need their own ID
    /// record" question this doesn't resolve).
    public const string RoleBorrower = "B";
    public const string RoleGuarantor = "G";

    /// ContractPhaseDomain: RQ=Requested, RN=Renounced, RF=Refused, AC=Active, CL=Closed,
    /// CA=Closed in advance. Mapped from LoanAccount.Status.
    public static string ContractPhaseCode(string loanAccountStatus) => loanAccountStatus switch
    {
        "PENDING_DISBURSEMENT" => "RQ",
        "ACTIVE" or "RESTRUCTURED" => "AC",
        "FULLY_PAID" => "CL",
        "CANCELLED" => "RN",
        "WRITTEN_OFF" => "CL",
        _ => "AC",
    };

    /// Installment ContractStatusDomain (distinct from ContractPhaseDomain — this is the
    /// performance/negative-status flag, not the lifecycle stage): DI=Dispute/Litigation contested,
    /// CR=Blocked or Closed due to Restructuring, WO=Write-off (BLW), PD=Past Due, or blank ("No
    /// info") for a normally performing account — there is no explicit "current/good standing" code
    /// in this domain, so blank is the correct value for one, not an omission.
    public static string? ContractStatusCode(bool isDisputed, bool wasRestructured, bool isWrittenOff, int daysOverdue)
    {
        if (isWrittenOff) return "WO";
        if (isDisputed) return "DI";
        if (wasRestructured) return "CR";
        if (daysOverdue > 0) return "PD";
        return null;
    }

    /// Installment ContractTypeDomain — a per-LoanProduct cooperative policy call
    /// (LoanProduct.CicContractTypeCode), defaulted to "12" (Personal Loan) as the most generic fit
    /// for an unconfigured product rather than left blank (Contract Type is Mandatory).
    public const string DefaultContractTypeCode = "12";
}
