namespace ZARI.Application.UnitTests.Features.Accounting.TaxCode;

using ZARI.Application.Features.Accounting.TaxCodes.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteTaxCodeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_TaxCode()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode();
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteTaxCodeCommand(taxCode.Code), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.TaxCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteTaxCodeCommand("VAT-MISSING"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode();
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("TAX_CODES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteTaxCodeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteTaxCodeCommand(taxCode.Code), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
