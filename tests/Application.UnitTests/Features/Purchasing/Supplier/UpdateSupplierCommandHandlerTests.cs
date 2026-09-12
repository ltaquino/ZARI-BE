namespace ZARI.Application.UnitTests.Features.Purchasing.Supplier;

using ZARI.Application.Features.Purchasing.Suppliers.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateSupplierCommandHandlerTests
{
    private static UpdateSupplierCommand Command(Guid id, string code = "SUP1", string? currencyId = null, Guid? apAccountId = null) =>
        new(id, code, "Updated Name", null, 60, currencyId, apAccountId, null, null, null, null, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Supplier()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(supplier.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SUPPLIERS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateSupplierCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(supplier.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier(code: "SUP1");
        var other = PurchasingTestFixtures.Supplier(code: "SUP2");
        db.Suppliers.AddRange(supplier, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(supplier.Id, code: "SUP2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Currency_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(supplier.Id, currencyId: "cur-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Currency.NotFound");
    }
}
