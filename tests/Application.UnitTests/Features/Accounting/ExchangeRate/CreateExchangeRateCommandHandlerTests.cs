namespace ZARI.Application.UnitTests.Features.Accounting.ExchangeRate;

using ZARI.Application.Features.Accounting.ExchangeRates.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateExchangeRateCommandHandlerTests
{
    private static CreateExchangeRateCommand Command(string currencyId) => new(currencyId, DateTimeOffset.UtcNow, 56.5m);

    [Fact]
    public async Task HandleAsync_Should_Create_ExchangeRate()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(currency.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RateToBase.Should().Be(56.5m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EXCHANGE_RATES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateExchangeRateCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command("cur-usd"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
