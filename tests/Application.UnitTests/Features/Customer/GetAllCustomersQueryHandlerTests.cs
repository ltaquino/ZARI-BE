namespace ZARI.Application.UnitTests.Features.Customer;

using ZARI.Application.Features.Customers.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllCustomersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Customers()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.Customers.Add(LoanTestFixtures.Customer(branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllCustomersQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCustomersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CUSTOMERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllCustomersQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllCustomersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
