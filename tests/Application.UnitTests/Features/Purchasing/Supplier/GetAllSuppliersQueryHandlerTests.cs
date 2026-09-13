namespace ZARI.Application.UnitTests.Features.Purchasing.Supplier;

using ZARI.Application.Features.Purchasing.Suppliers.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllSuppliersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Suppliers()
    {
        await using var db = TestDbContextFactory.Create();
        db.Suppliers.AddRange(PurchasingTestFixtures.Supplier(code: "SUP1"), PurchasingTestFixtures.Supplier(code: "SUP2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllSuppliersQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllSuppliersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SUPPLIERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllSuppliersQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllSuppliersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
