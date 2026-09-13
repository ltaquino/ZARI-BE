namespace ZARI.Application.UnitTests.Features.Accounting.TaxCode;

using ZARI.Application.Features.Accounting.TaxCodes.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllTaxCodesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_TaxCodes()
    {
        await using var db = TestDbContextFactory.Create();
        db.TaxCodes.AddRange(AccountingTestFixtures.TaxCode(code: "VAT12"), AccountingTestFixtures.TaxCode(code: "WHT2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllTaxCodesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllTaxCodesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("TAX_CODES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllTaxCodesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllTaxCodesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
