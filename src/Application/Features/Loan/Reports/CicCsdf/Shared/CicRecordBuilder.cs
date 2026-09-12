namespace ZARI.Application.Features.Loan.Reports.CicCsdf.Shared;

using System.Globalization;
using ZARI.Domain.Entities;

/// <summary>
/// Builds one `ID` (Individual, 123 fields) or `CI` (Installment Contract, 143 fields) record's
/// field array, in CIC CSDF v1.4's own exact field order — sourced directly from this folder's
/// "Fields in Excel version 1.4_test.xlsx" `test` sheet (ZARI-FE/frs/loan-cic/LoanCicContext.md
/// §3.5/§4), not re-derived from memory. Every field slot below is commented with its 1-based
/// position and CIC field name so this stays checkable against the Excel line by line. A `null`
/// entry becomes an empty pipe-delimited slot — CIC's own format allows this for every NM
/// (Not Mandatory) field, confirmed from the real sample file's trailing empty-pipe padding.
/// </summary>
internal static class CicRecordBuilder
{
    private const int IdFieldCount = 123;
    private const int CiFieldCount = 143;

    public static string?[] BuildIdRecord(Customer customer, string providerCode, DateTimeOffset subjectReferenceDate)
    {
        var fields = new string?[]
        {
            "ID",                                          // 1  Record Type
            providerCode,                                   // 2  Provider Code
            customer.BranchId,                               // 3  Branch Code
            Ddmmyyyy(subjectReferenceDate),                  // 4  Subject Reference Date
            customer.Id.ToString(),                          // 5  Provider Subject No
            null,                                            // 6  Title (TitleDomain is numeric-coded; Customer.Title has no FE field populating it yet — LoanCicContext.md §4.1)
            customer.FirstName,                              // 7  First Name
            customer.LastName,                               // 8  Last Name
            customer.MiddleName,                             // 9  Middle Name
            customer.Suffix,                                 // 10 Suffix
            null,                                            // 11 Nickname
            null,                                            // 12 Previous Last Name
            CicDomainCodes.GenderCode(customer.Sex),          // 13 Gender
            Ddmmyyyy(customer.DateOfBirth),                  // 14 Date of Birth
            customer.PlaceOfBirth,                           // 15 Place of Birth
            customer.CountryOfBirthCode,                     // 16 Country of Birth (Code)
            customer.NationalityCode,                        // 17 Nationality
            YesNo(customer.Resident),                        // 18 Resident
            CicDomainCodes.CivilStatusCode(customer.CivilStatus), // 19 Civil Status
            customer.DependentsCount?.ToString(CultureInfo.InvariantCulture), // 20 Number of Dependents
            customer.OwnsVehicle ? "1" : "0",                // 21 Car/s Owned
            null, null, null,                                // 22-24 Spouse First/Last/Middle Name
            null, null, null,                                // 25-27 Mother's Maiden First/Last/Middle Name
            null, null, null, null,                          // 28-31 Father First/Last/Middle Name, Suffix
            "MI",                                            // 32 Address 1: Address Type (Main/Residence)
            customer.Address,                                // 33 Address 1: FullAddress
            null,                                            // 34 Address 1: StreetNo
            customer.AddressPostalCode,                      // 35 Address 1: PostalCode
            customer.AddressSubdivision,                     // 36 Address 1: Subdivision
            customer.AddressBarangay,                        // 37 Address 1: Barangay
            customer.AddressCity,                            // 38 Address 1: City
            customer.AddressProvince,                        // 39 Address 1: Province
            customer.AddressCountryCode ?? "PH",              // 40 Address 1: Country
            CicDomainCodes.HouseOwnerLesseeCode(customer.AddressHouseOwnerOrLessee), // 41 Address 1: House Owner/Lessee
            Ddmmyyyy(customer.AddressOccupiedSince),          // 42 Address 1: Occupied Since
            null, null, null, null, null, null, null, null, null, null, // 43-52 Address 2 block (not captured)
            null,                                            // 53 Address 2: Occupied Since
            "TIN", customer.Tin,                             // 54-55 Identification 1: Type/Number
            "SSS", customer.SssOrGsisNo,                     // 56-57 Identification 2: Type/Number
            null, null,                                      // 58-59 Identification 3: Type/Number
            null, null, null, null, null, null,              // 60-65 ID 1 block (Type/Number/IssueDate/IssueCountry/ExpiryDate/IssuedBy)
            null, null, null, null, null, null,              // 66-71 ID 2 block
            null, null, null, null, null, null,              // 72-77 ID 3 block
            CicDomainCodes.ContactTypePhone, customer.Phone,  // 78-79 Contact 1: Type/Value
            CicDomainCodes.ContactTypeEmail, customer.Email,  // 80-81 Contact 2: Type/Value
            customer.Employer,                               // 82 Employment: Trade Name
            null,                                            // 83 Employment: TIN
            null,                                            // 84 Employment: Phone Number
            null,                                            // 85 Employment: PSIC (not sourced — LoanCicContext.md §6 #5)
            customer.NetIncomeLastYear?.ToString("0.00", CultureInfo.InvariantCulture), // 86 Employment: GrossIncome
            "Y",                                             // 87 Employment: Annual/Monthly Indicator (Annual — NetIncomeLastYear is annual)
            "PHP",                                           // 88 Employment: Currency
            null,                                            // 89 Employment: OccupationStatus
            Ddmmyyyy(customer.EmploymentSince),               // 90 Employment: DateHiredFrom
            null,                                            // 91 Employment: DateHiredTo
            null,                                            // 92 Employment: Occupation (PSOC — not sourced, same reason as PSIC)
            null, null, null, null, null, null, null, null, null, null, null, // 93-103 Sole Trader block 1 (not applicable — LoanCicContext.md §4.1)
            null,                                            // 104 Sole Trader 1: Occupied Since
            null, null, null, null, null, null, null, null, null, null, null, // 105-115 Sole Trader block 2
            null, null, null, null, null, null, null, null,  // 116-123 Sole Trader identification/contact
        };

        return AssertLength(fields, IdFieldCount, "ID");
    }

    public static string?[] BuildCiRecord(
        LoanAccount account,
        string providerCode,
        DateTimeOffset contractReferenceDate,
        decimal outstandingBalance,
        bool wasRestructured,
        DateTimeOffset? writeOffDate,
        int installmentsNumber,
        DateTimeOffset? firstDueDate,
        decimal installmentAmount,
        int outstandingPaymentsNumber,
        int overduePaymentsNumber,
        decimal overduePaymentsAmount,
        int daysOverdue,
        List<LoanCoMaker> coMakers)
    {
        var fields = new List<string?>(CiFieldCount)
        {
            "CI",                                            // 1  Record Type
            providerCode,                                    // 2  Provider Code
            account.BranchId,                                // 3  Branch Code
            Ddmmyyyy(contractReferenceDate),                 // 4  Contract Reference Date
            account.CustomerId.ToString(),                   // 5  Provider Subject No
            CicDomainCodes.RoleBorrower,                     // 6  Role
            account.LoanAcctNo,                              // 7  Provider Contract No
            account.LoanProduct.CicContractTypeCode ?? CicDomainCodes.DefaultContractTypeCode, // 8 Contract Type
            CicDomainCodes.ContractPhaseCode(account.Status), // 9  Contract Phase
            CicDomainCodes.ContractStatusCode(account.IsDisputed, wasRestructured, writeOffDate is not null, daysOverdue), // 10 Contract Status
            "PHP",                                           // 11 Currency
            "PHP",                                           // 12 Original Currency
            Ddmmyyyy(account.GrantDate),                     // 13 Contract Start Date
            Ddmmyyyy(account.GrantDate),                     // 14 Contract Request Date
            null,                                            // 15 Contract End Planned Date (last installment's due date — appended below once schedule lines are known by the caller if needed; left blank in v1)
            null,                                            // 16 Contract End Actual Date
            null,                                            // 17 Last Payment Date
            "0",                                             // 18 Reorganized Credit Code (0 = not re-organized; a restructured *new* account isn't itself a re-organization of a prior contract in CIC's sense)
            null,                                            // 19 Board Resolution flag
            account.PrincipalAmount.ToString("0.00", CultureInfo.InvariantCulture), // 20 Financed Amount
            installmentsNumber.ToString(CultureInfo.InvariantCulture), // 21 Installments Number
            null,                                            // 22 Transaction Type / Sub-facility
            null,                                            // 23 Purpose of credit
            CicDomainCodes.PaymentPeriodicityCode(account.RepaymentFrequency), // 24 Payment Periodicity
            null,                                            // 25 Payment Method (per-payment, not a contract-level attribute in ZARI's model)
            installmentAmount.ToString("0.00", CultureInfo.InvariantCulture), // 26 Monthly Payment Amount
            Ddmmyyyy(firstDueDate),                          // 27 First Payment Date
            null,                                            // 28 Last payment amount
            null,                                            // 29 Next Payment Date
            null,                                            // 30 Next Payment
            outstandingPaymentsNumber.ToString(CultureInfo.InvariantCulture), // 31 Outstanding Payments Number
            outstandingBalance.ToString("0.00", CultureInfo.InvariantCulture), // 32 Outstanding Balance
            overduePaymentsNumber.ToString(CultureInfo.InvariantCulture), // 33 Overdue Payments Number
            overduePaymentsAmount.ToString("0.00", CultureInfo.InvariantCulture), // 34 Overdue Payments Amount
            daysOverdue.ToString(CultureInfo.InvariantCulture), // 35 Overdue Days
            null, null, null, null, null, null,              // 36-41 Good Type/Value/New-Used/Brand/ManufacturingDate/RegistrationNumber (Good Type domain not sourced — LoanCicContext.md §4.2)
        };

        for (var i = 0; i < 6; i++)
        {
            var coMaker = i < coMakers.Count ? coMakers[i] : null;
            fields.Add(coMaker is null ? null : $"G{account.LoanAcctNo}-{i + 1}"); // Provider Guarantee No (n)
            fields.Add(coMaker?.CoMakerCustomerId?.ToString());                    // Provider Subject No (Guarantor)
            fields.Add(coMaker?.Name);                                            // Guarantor Name
            fields.Add(null);                                                     // Guaranteed Amount
            fields.Add(coMaker is null ? null : "PHP");                           // Currency
            fields.Add(null);                                                     // Validity Start Date
            fields.Add(null);                                                     // Validity End Date
            fields.Add(null);                                                     // Guarantee Type
            fields.Add(null);                                                     // Asset Code
            fields.Add(null);                                                     // Asset Description
            fields.Add(null);                                                     // Asset Location
            fields.Add(null);                                                     // Asset Appraised Value
            fields.Add(null);                                                     // Asset Registry External Link
            fields.Add(coMaker is null ? null : "1");                            // Customer Type (1 = Individual)
        }

        for (var i = 0; i < 6; i++)
        {
            var coMaker = i < coMakers.Count ? coMakers[i] : null;
            fields.Add(coMaker?.CoMakerCustomerId?.ToString()); // Provider Subject No (Linked Subject n)
            fields.Add(coMaker is null ? null : CicDomainCodes.RoleGuarantor); // Role
            fields.Add(coMaker?.Name);                          // Name of the Linked Subject
        }

        return AssertLength(fields.ToArray(), CiFieldCount, "CI");
    }

    private static string?[] AssertLength(string?[] fields, int expected, string recordType) =>
        fields.Length == expected
            ? fields
            : throw new InvalidOperationException($"CIC {recordType} record built with {fields.Length} fields, expected {expected} — CicRecordBuilder's field array is out of sync with the CSDF spec.");

    private static string? Ddmmyyyy(DateTimeOffset? date) => date?.ToString("ddMMyyyy", CultureInfo.InvariantCulture);

    private static string? YesNo(bool? value) => value switch { true => "1", false => "0", null => null };
}
