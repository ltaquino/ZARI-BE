namespace ZARI.Application.UnitTests.Features.Accounting.TaxCode;

using ZARI.Application.Features.Accounting.TaxCodes.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateTaxCodeCommandHandlerTests
{
    private static CreateTaxCodeCommand Command(string code = "VAT12", Guid? glAccountId = null) => new(code, "12% VAT", 12, "Vat", glAccountId);

    [Fact]
    public async Task HandleAsync_Should_Create_TaxCode()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("VAT12");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("TAX_CODES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateTaxCodeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.TaxCodes.Add(AccountingTestFixtures.TaxCode(code: "VAT12"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_GlAccount_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateTaxCodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(glAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
    }
}
