namespace ZARI.Application.UnitTests.Features.Accounting.ExchangeRate;

using ZARI.Application.Features.Accounting.ExchangeRates.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateExchangeRateCommandHandlerTests
{
    private static UpdateExchangeRateCommand Command(Guid id, string currencyId) => new(id, currencyId, DateTimeOffset.UtcNow, 58m);

    [Fact]
    public async Task HandleAsync_Should_Update_ExchangeRate()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        var rate = AccountingTestFixtures.ExchangeRate(currency.Id);
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(rate.Id, currency.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.ExchangeRates.FindAsync([rate.Id], TestContext.Current.CancellationToken))!.RateToBase.Should().Be(58m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), "cur-usd"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        var rate = AccountingTestFixtures.ExchangeRate(currency.Id);
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EXCHANGE_RATES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateExchangeRateCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(rate.Id, currency.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        var rate = AccountingTestFixtures.ExchangeRate(currency.Id);
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(rate.Id, "cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.NotFound");
    }
}
