namespace ZARI.Application.UnitTests.Features.Accounting.TaxCode;

using ZARI.Application.Features.Accounting.TaxCodes.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateTaxCodeCommandHandlerTests
{
    private static UpdateTaxCodeCommand Command(string code, Guid? glAccountId = null) => new(code, "Updated VAT", 10, "Vat", glAccountId);

    [Fact]
    public async Task HandleAsync_Should_Update_TaxCode()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode(code: "VAT12");
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(taxCode.Code), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.TaxCodes.FindAsync([taxCode.Code], TestContext.Current.CancellationToken))!.Rate.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("VAT-MISSING"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode(code: "VAT12");
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("TAX_CODES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateTaxCodeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(taxCode.Code), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_GlAccount_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var taxCode = AccountingTestFixtures.TaxCode(code: "VAT12");
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(taxCode.Code, glAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
    }
}
