namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

public sealed class Customer : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public string Status { get; set; } = default!;

    // The salesperson/account owner's display name — plain string, not a User FK, since Users
    // aren't a backend entity yet either.
    public string Owner { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Notes { get; set; }

    // Free-text cooperative member number — searchable from POS Mode alongside Name. No format
    // enforced; the business's existing member-card numbering (whatever it is) is entered as-is.
    public string? MemberNo { get; set; }

    // Mirrors Supplier.ApAccountId/PaymentTermsDays (Phase 17) exactly, on the AR side: an override
    // GL account for this customer's receivable (falls back to "1200" Accounts Receivable when
    // null) and a net-days default for Sales Invoice due dates (null = no default, purely manual).
    public Guid? ArAccountId { get; set; }
    public GlAccount? ArAccount { get; set; }
    public int? PaymentTermsDays { get; set; }

    // Discount Scheme (ZARI-FE/frs/sales/DiscountSchemeContext.md §2.2): a standing % suggested by
    // default on every new Sales Order/Invoice line for this customer — a suggestion only, always
    // freely overridable, never enforced.
    public decimal? StandingDiscountPct { get; set; }

    // "Basic Credit Data" fields required by RA 9510 (the Credit Information System Act) as
    // implemented for cooperatives by CDA Memorandum Circular 2019-01 — captured here so a
    // compliance officer can produce the CIC submission extract (see
    // Features/Loan/Reports/CisaCreditData). All nullable/optional: only loan borrowers need this
    // filled in, not every Customer (walk-in POS buyers, suppliers-as-customers, etc.). MC 2019-01
    // does not publish the actual CIC file format, so this is data CAPTURE only — no live CIC
    // submission integration (ZARI-FE/frs/loan/LoanModuleContext.md §6.4's caution applies:
    // confirm exact CIC extract format with the cooperative's compliance officer before relying on
    // the export as submission-ready).
    public string? Tin { get; set; }
    public string? SssOrGsisNo { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? CivilStatus { get; set; }
    public int? DependentsCount { get; set; }
    public string? Employer { get; set; }
    public string? EmployerPosition { get; set; }
    public decimal? NetIncomeLastYear { get; set; }
    // "Residence history (2 years)" / "Employment history (5 years)" per MC 2019-01 — modeled as a
    // since-date plus free-text prior history rather than a separate child-record list, since this
    // is a point-in-time profile snapshot for the CIC extract, not something ZARI itself needs to
    // query/report on by individual prior address.
    public DateTimeOffset? ResidenceSince { get; set; }
    public string? PriorResidenceHistory { get; set; }
    public DateTimeOffset? EmploymentSince { get; set; }
    public string? PriorEmploymentHistory { get; set; }
    public string? HousingStatus { get; set; }
    public bool OwnsVehicle { get; set; }
    public string? BankAccountInfo { get; set; }
    public string? OtherAssetsNotes { get; set; }

    // MC 2019-01's mandatory disclosure: a member must be notified their basic credit data will be
    // submitted to the CIC and consent captured — typically via a clause on the loan application.
    public bool DataSharingConsent { get; set; }
    public DateTimeOffset? DataSharingConsentDate { get; set; }

    // CIC's real CSDF v1.4 "ID" (Individual) record fields (ZARI-FE/frs/loan-cic/LoanCicContext.md
    // §4.1) that the CISA block above doesn't cover. All nullable/optional, same rule as the block
    // above — only loan borrowers actually reported to CIC need these filled in. `Name` stays the
    // single free-text field used everywhere else in the app (Sales, POS, etc.); these are captured
    // in addition to it, specifically for the CIC export, rather than replacing it.
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Suffix { get; set; }
    public string? PlaceOfBirth { get; set; }
    // CIC's CountryDomain 2-letter code (e.g. "PH") — same domain used for both fields.
    public string? CountryOfBirthCode { get; set; }
    public string? NationalityCode { get; set; }
    public bool? Resident { get; set; }
    // Structured breakdown of the single free-text `Address` above, for CIC's Address 1 block.
    public string? AddressSubdivision { get; set; }
    public string? AddressBarangay { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressProvince { get; set; }
    public string? AddressPostalCode { get; set; }
    public string? AddressCountryCode { get; set; }
    // "OWN" / "RENT" / "LEASE" / "OTHER" — enum-shaped string (this codebase's usual convention),
    // mapped to CIC's numeric HouseOwnerLesseeType code only at export time.
    public string? AddressHouseOwnerOrLessee { get; set; }
    public DateTimeOffset? AddressOccupiedSince { get; set; }
}
