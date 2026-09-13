namespace ZARI.Application.UnitTests.Features.SystemModule.Currency;

using ZARI.Application.Features.SystemModule.Currencies.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteCurrencyCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Currency()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCurrencyCommand(currency.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Currencies.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCurrencyCommand("cur-missing"), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("CURRENCIES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteCurrencyCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteCurrencyCommand(currency.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Is_Base_Currency()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-php", code: "PHP");
        db.Currencies.Add(currency);
        db.Companies.Add(SystemModuleTestFixtures.Company(baseCurrencyId: currency.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCurrencyCommand(currency.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.IsBaseCurrency");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Has_ExchangeRates()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        db.ExchangeRates.Add(new ExchangeRate { CurrencyId = currency.Id, RateDate = DateTimeOffset.UtcNow, RateToBase = 56.5m });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCurrencyCommand(currency.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.HasExchangeRates");
    }
}
