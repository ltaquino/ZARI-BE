namespace ZARI.Application.UnitTests.Features.Loan.Reports.CicCsdf.Shared;

using ZARI.Application.Features.Loan.Reports.CicCsdf.Shared;

public sealed class CicDomainCodesTests
{
    [Theory]
    [InlineData("M", "M")]
    [InlineData("MALE", "M")]
    [InlineData("f", "F")]
    [InlineData("Female", "F")]
    [InlineData(null, null)]
    [InlineData("unknown", null)]
    public void GenderCode_Should_Map_To_CIC_GenderDomain(string? sex, string? expected) =>
        CicDomainCodes.GenderCode(sex).Should().Be(expected);

    [Theory]
    [InlineData("SINGLE", "1")]
    [InlineData("Married", "2")]
    [InlineData("divorced", "3")]
    [InlineData("SEPARATED", "3")]
    [InlineData("Widow", "4")]
    [InlineData("widowed", "4")]
    [InlineData(null, null)]
    public void CivilStatusCode_Should_Map_To_CIC_CivilStatusDomain(string? civilStatus, string? expected) =>
        CicDomainCodes.CivilStatusCode(civilStatus).Should().Be(expected);

    [Theory]
    [InlineData("OWN", "1")]
    [InlineData("rent", "2")]
    [InlineData("Lease", "3")]
    [InlineData("OTHER", "4")]
    [InlineData(null, null)]
    public void HouseOwnerLesseeCode_Should_Map_To_CIC_HouseOwnerLesseeType(string? value, string? expected) =>
        CicDomainCodes.HouseOwnerLesseeCode(value).Should().Be(expected);

    [Theory]
    [InlineData("MONTHLY", "M")]
    [InlineData("SEMI_MONTHLY", "F")]
    [InlineData("WEEKLY", "W")]
    [InlineData("DAILY", null)]
    public void PaymentPeriodicityCode_Should_Map_To_CIC_PaymentPeriodicityDomain(string repaymentFrequency, string? expected) =>
        CicDomainCodes.PaymentPeriodicityCode(repaymentFrequency).Should().Be(expected);

    [Theory]
    [InlineData("CASH", "CAS")]
    [InlineData("CARD", "CCR")]
    [InlineData("GIFT_CHECK", "CHQ")]
    [InlineData("SOMETHING_ELSE", "OTH")]
    [InlineData(null, "OTH")]
    public void PaymentMethodCode_Should_Map_To_CIC_PaymentMethodDomain(string? paymentMethodCode, string expected) =>
        CicDomainCodes.PaymentMethodCode(paymentMethodCode).Should().Be(expected);

    [Theory]
    [InlineData("PENDING_DISBURSEMENT", "RQ")]
    [InlineData("ACTIVE", "AC")]
    [InlineData("RESTRUCTURED", "AC")]
    [InlineData("FULLY_PAID", "CL")]
    [InlineData("CANCELLED", "RN")]
    [InlineData("WRITTEN_OFF", "CL")]
    public void ContractPhaseCode_Should_Map_To_CIC_ContractPhaseDomain(string status, string expected) =>
        CicDomainCodes.ContractPhaseCode(status).Should().Be(expected);

    [Fact]
    public void ContractStatusCode_Should_Prioritize_WrittenOff_Over_Everything_Else() =>
        CicDomainCodes.ContractStatusCode(isDisputed: true, wasRestructured: true, isWrittenOff: true, daysOverdue: 90).Should().Be("WO");

    [Fact]
    public void ContractStatusCode_Should_Prioritize_Disputed_Over_Restructured_And_Overdue() =>
        CicDomainCodes.ContractStatusCode(isDisputed: true, wasRestructured: true, isWrittenOff: false, daysOverdue: 90).Should().Be("DI");

    [Fact]
    public void ContractStatusCode_Should_Flag_Restructured_When_Not_Disputed_Or_WrittenOff() =>
        CicDomainCodes.ContractStatusCode(isDisputed: false, wasRestructured: true, isWrittenOff: false, daysOverdue: 0).Should().Be("CR");

    [Fact]
    public void ContractStatusCode_Should_Flag_Past_Due_When_Overdue_And_Nothing_Else_Applies() =>
        CicDomainCodes.ContractStatusCode(isDisputed: false, wasRestructured: false, isWrittenOff: false, daysOverdue: 5).Should().Be("PD");

    [Fact]
    public void ContractStatusCode_Should_Be_Null_For_A_Normally_Performing_Account() =>
        CicDomainCodes.ContractStatusCode(isDisputed: false, wasRestructured: false, isWrittenOff: false, daysOverdue: 0).Should().BeNull();
}
