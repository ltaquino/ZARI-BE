namespace ZARI.Application.UnitTests.Features.SystemModule.Currency;

using ZARI.Application.Features.SystemModule.Currencies.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllCurrenciesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Currencies_Ordered_By_Code()
    {
        await using var db = TestDbContextFactory.Create();
        db.Currencies.AddRange(
            SystemModuleTestFixtures.Currency(id: "cur-usd", code: "USD"),
            SystemModuleTestFixtures.Currency(id: "cur-eur", code: "EUR"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllCurrenciesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCurrenciesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(c => c.Code).Should().ContainInOrder("EUR", "USD");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CURRENCIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllCurrenciesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllCurrenciesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
