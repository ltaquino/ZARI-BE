namespace ZARI.Application.UnitTests.Features.Customer.CreditRecord;

using ZARI.Application.Features.Customers.CreditRecords.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCustomerCreditRecordCommandHandlerTests
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

    private static CreateCustomerCreditRecordCommand Command(Guid customerId) =>
        new(customerId, "DEFAULT", "defaulted on a prior coop loan", DateTimeOffset.UtcNow.AddYears(-1), 5000, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Create_Record()
    {
        var (db, customer) = await Seed();
        var handler = new CreateCustomerCreditRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerName.Should().Be(customer.Name);
        result.Value.RecordType.Should().Be("DEFAULT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCustomerCreditRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, customer) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.Create, customer.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCustomerCreditRecordCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(customer.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
