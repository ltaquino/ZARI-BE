namespace ZARI.Application.UnitTests.Features.Purchasing.Supplier;

using ZARI.Application.Features.Purchasing.Suppliers.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateSupplierCommandHandlerTests
{
    private static CreateSupplierCommand Command(string code = "SUP1", string? currencyId = null, Guid? apAccountId = null) =>
        new(code, "Test Supplier", null, 30, currencyId, apAccountId, null, null, null, null, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Supplier()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SUP1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SUPPLIERS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateSupplierCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.Add(PurchasingTestFixtures.Supplier(code: "SUP1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(currencyId: "cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_ApAccount_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(apAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
    }
}
