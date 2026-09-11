namespace ZARI.Application.UnitTests.Features.Customer.CreditRecord;

using ZARI.Application.Features.Customers.CreditRecords.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllCustomerCreditRecordsQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Customer customerA, Customer customerB)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customerA = LoanTestFixtures.Customer(branch.Id, "Customer A");
        var customerB = LoanTestFixtures.Customer(branch.Id, "Customer B");
        db.Customers.Add(customerA);
        db.Customers.Add(customerB);
        db.CustomerCreditRecords.Add(new CustomerCreditRecord { CustomerId = customerA.Id, RecordType = "DEFAULT", Description = "a", RecordDate = DateTimeOffset.UtcNow.AddDays(-1) });
        db.CustomerCreditRecords.Add(new CustomerCreditRecord { CustomerId = customerB.Id, RecordType = "BOUNCED_CHECK", Description = "b", RecordDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, customerA, customerB);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_All_Records_When_No_Filter()
    {
        var (db, _, _) = await Seed();
        var handler = new GetAllCustomerCreditRecordsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCustomerCreditRecordsQuery(null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_CustomerId()
    {
        var (db, customerA, _) = await Seed();
        var handler = new GetAllCustomerCreditRecordsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllCustomerCreditRecordsQuery(customerA.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.CustomerId == customerA.Id);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("CUSTOMER_CREDIT_RECORDS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllCustomerCreditRecordsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllCustomerCreditRecordsQuery(null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
