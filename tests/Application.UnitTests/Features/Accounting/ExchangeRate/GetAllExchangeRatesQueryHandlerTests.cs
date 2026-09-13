namespace ZARI.Application.UnitTests.Features.Accounting.ExchangeRate;

using ZARI.Application.Features.Accounting.ExchangeRates.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllExchangeRatesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_ExchangeRates()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        db.ExchangeRates.Add(AccountingTestFixtures.ExchangeRate(currency.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllExchangeRatesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllExchangeRatesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EXCHANGE_RATES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllExchangeRatesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllExchangeRatesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
