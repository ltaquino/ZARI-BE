namespace ZARI.Application.UnitTests.Features.Accounting.ExchangeRate;

using ZARI.Application.Features.Accounting.ExchangeRates.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetExchangeRateQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_ExchangeRate_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        var rate = AccountingTestFixtures.ExchangeRate(currency.Id, rateToBase: 56.5m);
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetExchangeRateQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetExchangeRateQuery(rate.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RateToBase.Should().Be(56.5m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetExchangeRateQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetExchangeRateQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EXCHANGE_RATES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetExchangeRateQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetExchangeRateQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
