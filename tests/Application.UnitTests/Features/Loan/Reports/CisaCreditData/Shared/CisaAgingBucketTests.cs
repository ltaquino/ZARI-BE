namespace ZARI.Application.UnitTests.Features.Loan.Reports.CisaCreditData.Shared;

using ZARI.Application.Features.Loan.Reports.CisaCreditData.Shared;

public sealed class CisaAgingBucketTests
{
    [Theory]
    [InlineData(0, "CURRENT")]
    [InlineData(-5, "CURRENT")]
    [InlineData(1, "1-30")]
    [InlineData(30, "1-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "91-120")]
    [InlineData(120, "91-120")]
    [InlineData(121, "121-150")]
    [InlineData(150, "121-150")]
    [InlineData(151, "151-180")]
    [InlineData(180, "151-180")]
    [InlineData(181, "OVER-180")]
    [InlineData(500, "OVER-180")]
    public void Of_Should_Map_DaysOverdue_To_Correct_Bucket(int daysOverdue, string expectedBucket)
    {
        CisaAgingBucket.Of(daysOverdue).Should().Be(expectedBucket);
    }
}
