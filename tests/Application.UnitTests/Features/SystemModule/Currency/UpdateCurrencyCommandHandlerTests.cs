namespace ZARI.Application.UnitTests.Features.SystemModule.Currency;

using ZARI.Application.Features.SystemModule.Currencies.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateCurrencyCommandHandlerTests
{
    private static UpdateCurrencyCommand Command(string id, string code = "USD") => new(id, code, "US Dollar Updated", "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Currency()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(currency.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Currencies.FindAsync([currency.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CURRENCIES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateCurrencyCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(currency.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts_With_Another_Currency()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        var other = SystemModuleTestFixtures.Currency(id: "cur-eur", code: "EUR");
        db.Currencies.AddRange(currency, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(currency.Id, code: "EUR"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
