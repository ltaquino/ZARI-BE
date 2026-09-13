namespace ZARI.Application.UnitTests.Features.Customer.CreditRecord;

using ZARI.Application.Features.Customers.CreditRecords.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateCustomerCreditRecordCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, CustomerCreditRecord record, Customer customer)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var record = new CustomerCreditRecord
        {
            CustomerId = customer.Id, RecordType = "DEFAULT", Description = "original", RecordDate = DateTimeOffset.UtcNow.AddYears(-1)
        };
        db.CustomerCreditRecords.Add(record);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, record, customer);
    }

    private static UpdateCustomerCreditRecordCommand Command(Guid id, string description = "updated") =>
        new(id, "BOUNCED_CHECK", description, DateTimeOffset.UtcNow.AddMonths(-6), 1000, "updated remarks");

    [Fact]
    public async Task HandleAsync_Should_Update_Record()
    {
        var (db, record, _) = await Seed();
        var handler = new UpdateCustomerCreditRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(record.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.CustomerCreditRecords.FindAsync([record.Id], TestContext.Current.CancellationToken))!.Description.Should().Be("updated");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCustomerCreditRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, record, customer) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.Edit, customer.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateCustomerCreditRecordCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(record.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
