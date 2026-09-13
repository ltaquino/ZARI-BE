namespace ZARI.Application.UnitTests.Features.Purchasing.Supplier;

using ZARI.Application.Features.Purchasing.Suppliers.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteSupplierCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Supplier()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSupplierCommand(supplier.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteSupplierCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSupplierCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("SUPPLIERS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteSupplierCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteSupplierCommand(supplier.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
