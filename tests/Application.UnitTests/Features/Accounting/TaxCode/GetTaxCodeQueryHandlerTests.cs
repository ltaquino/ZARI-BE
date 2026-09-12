namespace ZARI.Application.UnitTests.Features.Accounting.TaxCode;

using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetTaxCodeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_TaxCode_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode();
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTaxCodeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTaxCodeQuery(taxCode.Code), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rate.Should().Be(taxCode.Rate);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetTaxCodeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTaxCodeQuery("VAT-MISSING"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("TAX_CODES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetTaxCodeQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetTaxCodeQuery("VAT12"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
