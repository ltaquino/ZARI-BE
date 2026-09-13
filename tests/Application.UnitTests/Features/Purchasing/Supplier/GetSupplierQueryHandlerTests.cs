namespace ZARI.Application.UnitTests.Features.Purchasing.Supplier;

using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetSupplierQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Supplier_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSupplierQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSupplierQuery(supplier.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(supplier.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSupplierQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSupplierQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SUPPLIERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetSupplierQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetSupplierQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
