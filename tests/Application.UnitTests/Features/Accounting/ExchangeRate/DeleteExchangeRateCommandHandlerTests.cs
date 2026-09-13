namespace ZARI.Application.UnitTests.Features.Accounting.ExchangeRate;

using ZARI.Application.Features.Accounting.ExchangeRates.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteExchangeRateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_ExchangeRate()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        var rate = AccountingTestFixtures.ExchangeRate(currency.Id);
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteExchangeRateCommand(rate.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.ExchangeRates.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteExchangeRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteExchangeRateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("EXCHANGE_RATES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteExchangeRateCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteExchangeRateCommand(rate.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
