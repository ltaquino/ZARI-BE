namespace ZARI.Application.UnitTests.Features.Customer;

using ZARI.Application.Features.Customers.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateCustomerCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Customer customer, string branchId)> Seed(bool preExistingConsent = false, DateTimeOffset? preExistingConsentDate = null)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        customer.DataSharingConsent = preExistingConsent;
        customer.DataSharingConsentDate = preExistingConsentDate;
        db.Customers.Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, customer, branch.Id);
    }

    private static UpdateCustomerCommand Command(Guid id, string branchId, bool dataSharingConsent = false, DateTimeOffset? consentDate = null, string name = "Updated Name") =>
        new(id, name, "individual", "juan@example.com", "0917-000-0000", branchId, "active", "tester", "Test Address", null, null, null, null,
            DataSharingConsent: dataSharingConsent, DataSharingConsentDate: consentDate);

    [Fact]
    public async Task HandleAsync_Should_Update_Customer()
    {
        var (db, customer, branchId) = await Seed();
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id, branchId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Customers.FindAsync([customer.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated Name");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Preserve_Original_ConsentDate_When_Already_Consented()
    {
        var originalDate = DateTimeOffset.UtcNow.AddDays(-30);
        var (db, customer, branchId) = await Seed(preExistingConsent: true, preExistingConsentDate: originalDate);
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id, branchId, dataSharingConsent: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Customers.FindAsync([customer.Id], TestContext.Current.CancellationToken))!.DataSharingConsentDate.Should().Be(originalDate);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Stamp_New_ConsentDate_When_Consent_Newly_Given()
    {
        var (db, customer, branchId) = await Seed(preExistingConsent: false);
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id, branchId, dataSharingConsent: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Customers.FindAsync([customer.Id], TestContext.Current.CancellationToken))!.DataSharingConsentDate.Should().NotBeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Clear_ConsentDate_When_Consent_Withdrawn()
    {
        var (db, customer, branchId) = await Seed(preExistingConsent: true, preExistingConsentDate: DateTimeOffset.UtcNow.AddDays(-30));
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id, branchId, dataSharingConsent: false), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Customers.FindAsync([customer.Id], TestContext.Current.CancellationToken))!.DataSharingConsentDate.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Persist_Cic_Id_Record_Fields()
    {
        var (db, customer, branchId) = await Seed();
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var command = Command(customer.Id, branchId) with
        {
            FirstName = "Juan", LastName = "Dela Cruz", AddressCity = "Ibaan", AddressProvince = "Batangas",
            AddressHouseOwnerOrLessee = "RENT", Resident = true
        };

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Customers.FindAsync([customer.Id], TestContext.Current.CancellationToken);
        updated!.FirstName.Should().Be("Juan");
        updated.AddressCity.Should().Be("Ibaan");
        updated.AddressHouseOwnerOrLessee.Should().Be("RENT");
        updated.Resident.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, _, branchId) = await Seed();
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, customer, branchId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateCustomerCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(customer.Id, branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, customer, _) = await Seed();
        var handler = new UpdateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(customer.Id, "br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }
}
