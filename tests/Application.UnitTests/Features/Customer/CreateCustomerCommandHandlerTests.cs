namespace ZARI.Application.UnitTests.Features.Customer;

using ZARI.Application.Features.Customers.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateCustomerCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id);
    }

    private static CreateCustomerCommand Command(string branchId, bool dataSharingConsent = false, DateTimeOffset? consentDate = null) =>
        new("Juan Dela Cruz", "individual", "juan@example.com", "0917-000-0000", branchId, "active", "tester", "Test Address", null, null, null, null,
            DataSharingConsent: dataSharingConsent, DataSharingConsentDate: consentDate);

    [Fact]
    public async Task HandleAsync_Should_Create_Customer()
    {
        var (db, branchId) = await Seed();
        var handler = new CreateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Juan Dela Cruz");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Stamp_ConsentDate_When_Consent_Given_Without_Explicit_Date()
    {
        var (db, branchId) = await Seed();
        var handler = new CreateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId, dataSharingConsent: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DataSharingConsentDate.Should().NotBeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Leave_ConsentDate_Null_When_No_Consent()
    {
        var (db, branchId) = await Seed();
        var handler = new CreateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId, dataSharingConsent: false), TestContext.Current.CancellationToken);

        result.Value!.DataSharingConsentDate.Should().BeNull();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCustomerCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _) = await Seed();
        var handler = new CreateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_ArAccount_Not_Found()
    {
        var (db, branchId) = await Seed();
        var handler = new CreateCustomerCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var command = Command(branchId) with { ArAccountId = Guid.NewGuid() };

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
        await db.DisposeAsync();
    }
}
