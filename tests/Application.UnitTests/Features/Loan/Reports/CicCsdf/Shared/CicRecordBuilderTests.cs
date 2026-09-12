namespace ZARI.Application.UnitTests.Features.Loan.Reports.CicCsdf.Shared;

using System.Globalization;
using ZARI.Application.Features.Loan.Reports.CicCsdf.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class CicRecordBuilderTests
{
    private static Customer BuildCustomer()
    {
        var customer = LoanTestFixtures.Customer("br-1", "Juan Dela Cruz");
        customer.FirstName = "Juan";
        customer.LastName = "Dela Cruz";
        customer.MiddleName = "Santos";
        customer.Suffix = "Jr";
        customer.Sex = "Male";
        customer.CivilStatus = "Married";
        customer.Tin = "123-456-789";
        customer.SssOrGsisNo = "34-1234567-8";
        return customer;
    }

    [Fact]
    public void BuildIdRecord_Should_Produce_Exactly_123_Fields()
    {
        var fields = CicRecordBuilder.BuildIdRecord(BuildCustomer(), "CO123456", DateTimeOffset.UtcNow);

        fields.Should().HaveCount(123);
    }

    [Fact]
    public void BuildIdRecord_Should_Populate_Core_Subject_Fields_At_Their_Spec_Positions()
    {
        var customer = BuildCustomer();
        var referenceDate = new DateTimeOffset(2026, 5, 30, 0, 0, 0, TimeSpan.Zero);

        var fields = CicRecordBuilder.BuildIdRecord(customer, "CO123456", referenceDate);

        fields[0].Should().Be("ID");                          // Record Type
        fields[1].Should().Be("CO123456");                    // Provider Code
        fields[2].Should().Be(customer.BranchId);              // Branch Code
        fields[3].Should().Be("30052026");                    // Subject Reference Date, DDMMYYYY
        fields[4].Should().Be(customer.Id.ToString());        // Provider Subject No
        fields[6].Should().Be("Juan");                        // First Name
        fields[7].Should().Be("Dela Cruz");                   // Last Name
        fields[8].Should().Be("Santos");                      // Middle Name
        fields[9].Should().Be("Jr");                          // Suffix
        fields[12].Should().Be("M");                          // Gender
        fields[18].Should().Be("2");                          // Civil Status (Married)
        fields[31].Should().Be("MI");                         // Address 1: Address Type
        fields[32].Should().Be(customer.Address);              // Address 1: FullAddress
        fields[53].Should().Be("TIN");                        // Identification 1: Type
        fields[54].Should().Be(customer.Tin);                  // Identification 1: Number
        fields[55].Should().Be("SSS");                        // Identification 2: Type
        fields[56].Should().Be(customer.SssOrGsisNo);          // Identification 2: Number
        fields[77].Should().Be("3");                          // Contact 1: Type (mobile phone)
        fields[78].Should().Be(customer.Phone);                // Contact 1: Value
        fields[79].Should().Be("7");                          // Contact 2: Type (e-mail)
        fields[80].Should().Be(customer.Email);                // Contact 2: Value
    }

    [Fact]
    public void BuildCiRecord_Should_Produce_Exactly_143_Fields()
    {
        var customer = BuildCustomer();
        var product = LoanTestFixtures.LoanProduct();
        var account = LoanTestFixtures.LoanAccount("br-1", customer.Id, product.Id, withSchedule: false);
        account.LoanProduct = product;

        var fields = CicRecordBuilder.BuildCiRecord(account, "CO123456", DateTimeOffset.UtcNow, 1000, false, null, 6, DateTimeOffset.UtcNow, 500, 6, 0, 0, 0, []);

        fields.Should().HaveCount(143);
    }

    [Fact]
    public void BuildCiRecord_Should_Populate_Core_Contract_Fields_At_Their_Spec_Positions()
    {
        var customer = BuildCustomer();
        var product = LoanTestFixtures.LoanProduct();
        var account = LoanTestFixtures.LoanAccount("br-1", customer.Id, product.Id, status: "ACTIVE", withSchedule: false);
        account.LoanProduct = product;
        var firstDueDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var fields = CicRecordBuilder.BuildCiRecord(account, "CO123456", DateTimeOffset.UtcNow, 9000m, false, null, 6, firstDueDate, 2100.50m, 5, 1, 300m, 12, []);

        fields[0].Should().Be("CI");                           // Record Type
        fields[1].Should().Be("CO123456");                     // Provider Code
        fields[4].Should().Be(account.CustomerId.ToString());   // Provider Subject No
        fields[5].Should().Be("B");                            // Role (Borrower)
        fields[6].Should().Be(account.LoanAcctNo);              // Provider Contract No
        fields[7].Should().Be("12");                           // Contract Type (default, product not configured)
        fields[8].Should().Be("AC");                           // Contract Phase (ACTIVE)
        fields[9].Should().Be("PD");                           // Contract Status (overdue, not disputed/restructured/written-off)
        fields[19].Should().Be(account.PrincipalAmount.ToString("0.00", CultureInfo.InvariantCulture)); // Financed Amount
        fields[20].Should().Be("6");                           // Installments Number
        fields[23].Should().Be("M");                           // Payment Periodicity (MONTHLY)
        fields[25].Should().Be("2100.50");                     // Monthly Payment Amount
        fields[26].Should().Be("01062026");                    // First Payment Date, DDMMYYYY
        fields[30].Should().Be("5");                           // Outstanding Payments Number
        fields[31].Should().Be("9000.00");                     // Outstanding Balance
        fields[32].Should().Be("1");                           // Overdue Payments Number
        fields[33].Should().Be("300.00");                      // Overdue Payments Amount
        fields[34].Should().Be("12");                          // Overdue Days
    }

    [Fact]
    public void BuildCiRecord_Should_Use_Products_Own_Cic_Contract_Type_Code_When_Set()
    {
        var customer = BuildCustomer();
        var product = LoanTestFixtures.LoanProduct();
        product.CicContractTypeCode = "20";
        var account = LoanTestFixtures.LoanAccount("br-1", customer.Id, product.Id, withSchedule: false);
        account.LoanProduct = product;

        var fields = CicRecordBuilder.BuildCiRecord(account, "CO123456", DateTimeOffset.UtcNow, 0, false, null, 0, null, 0, 0, 0, 0, 0, []);

        fields[7].Should().Be("20");
    }

    [Fact]
    public void BuildCiRecord_Should_Populate_First_Guarantee_And_Linked_Subject_Block_From_First_CoMaker()
    {
        var customer = BuildCustomer();
        var product = LoanTestFixtures.LoanProduct();
        var account = LoanTestFixtures.LoanAccount("br-1", customer.Id, product.Id, withSchedule: false);
        account.LoanProduct = product;
        var coMakerCustomerId = Guid.NewGuid();
        var coMaker = new LoanCoMaker { LoanApplicationId = Guid.NewGuid(), CoMakerCustomerId = coMakerCustomerId, Name = "Maria Guarantor" };

        var fields = CicRecordBuilder.BuildCiRecord(account, "CO123456", DateTimeOffset.UtcNow, 0, false, null, 0, null, 0, 0, 0, 0, 0, [coMaker]);

        fields[42].Should().Be(coMakerCustomerId.ToString());  // Guarantee block 1: Provider Subject No (Guarantor)
        fields[43].Should().Be("Maria Guarantor");              // Guarantee block 1: Guarantor Name
        fields[125].Should().Be(coMakerCustomerId.ToString()); // Linked Subject 1: Provider Subject No
        fields[126].Should().Be("G");                          // Linked Subject 1: Role
        fields[127].Should().Be("Maria Guarantor");             // Linked Subject 1: Name
    }

    [Fact]
    public void BuildCiRecord_Should_Leave_Guarantee_And_Linked_Subject_Blocks_Blank_When_No_CoMakers()
    {
        var customer = BuildCustomer();
        var product = LoanTestFixtures.LoanProduct();
        var account = LoanTestFixtures.LoanAccount("br-1", customer.Id, product.Id, withSchedule: false);
        account.LoanProduct = product;

        var fields = CicRecordBuilder.BuildCiRecord(account, "CO123456", DateTimeOffset.UtcNow, 0, false, null, 0, null, 0, 0, 0, 0, 0, []);

        fields[42].Should().BeNull();
        fields[125].Should().BeNull();
    }
}
