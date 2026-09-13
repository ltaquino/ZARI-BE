namespace ZARI.Application.UnitTests.Features.Customer;

using ZARI.Application.Features.Customers.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteCustomerCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Customer customer)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, customer);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Customer()
    {
        var (db, customer) = await Seed();
        var handler = new DeleteCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCustomerCommand(customer.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Customers.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteCustomerCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, customer) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Delete, customer.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteCustomerCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteCustomerCommand(customer.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
