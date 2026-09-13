namespace ZARI.Application.UnitTests.Features.SystemModule.Currency;

using ZARI.Application.Features.SystemModule.Currencies.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateCurrencyCommandHandlerTests
{
    private static CreateCurrencyCommand Command(string code = "USD") => new(code, "US Dollar", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Currency()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("USD");
        db.Currencies.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CURRENCIES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCurrencyCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.Currencies.Add(SystemModuleTestFixtures.Currency(code: "USD"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCurrencyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
