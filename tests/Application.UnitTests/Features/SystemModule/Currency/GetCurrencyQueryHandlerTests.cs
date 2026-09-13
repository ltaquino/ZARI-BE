namespace ZARI.Application.UnitTests.Features.SystemModule.Currency;

using ZARI.Application.Features.SystemModule.Currencies.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetCurrencyQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Currency_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var currency = SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD");
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCurrencyQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCurrencyQuery(currency.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("USD");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCurrencyQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCurrencyQuery("cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CURRENCIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCurrencyQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCurrencyQuery("cur-usd"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
