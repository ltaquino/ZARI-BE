namespace ZARI.Application.UnitTests.Features.SystemModule.DeviceSettings;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.SystemModule.DeviceSettings;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class GetDeviceSettingsQueryHandlerTests
{
    private static async Task<ZARI.Infrastructure.Persistence.AppDbContext> Seed()
    {
        var db = TestDbContextFactory.Create();
        var company = SystemModuleTestFixtures.Company();
        company.TradeName = "Coop Mart";
        company.MaxUnapprovedDiscountPct = 10;
        db.Companies.Add(company);
        db.Branches.AddRange(LoanTestFixtures.Branch("br-1"), LoanTestFixtures.Branch("br-2"));
        var closed = LoanTestFixtures.Branch("br-3");
        closed.Status = "inactive";
        db.Branches.Add(closed);
        db.UserBranches.Add(new UserBranch { UserId = "cashier", BranchId = "br-2" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return db;
    }

    private static ICurrentUser User(string? id)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(id);
        return user;
    }

    [Fact]
    public async Task HandleAsync_Should_Return_The_Discount_Threshold_And_The_Users_Own_Branches()
    {
        await using var db = await Seed();

        var result = await new GetDeviceSettingsQueryHandler(db, User("cashier")).HandleAsync(new GetDeviceSettingsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MaxUnapprovedDiscountPct.Should().Be(10);
        result.Value.TradeName.Should().Be("Coop Mart");
        result.Value.Branches.Select(b => b.Id).Should().Equal("br-2");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Every_Active_Branch_To_A_User_With_No_Branch_Assignment()
    {
        await using var db = await Seed();

        var result = await new GetDeviceSettingsQueryHandler(db, User("admin")).HandleAsync(new GetDeviceSettingsQuery(), TestContext.Current.CancellationToken);

        result.Value!.Branches.Select(b => b.Id).Should().BeEquivalentTo(["br-1", "br-2"]);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Signed_In()
    {
        await using var db = await Seed();

        var result = await new GetDeviceSettingsQueryHandler(db, User(null)).HandleAsync(new GetDeviceSettingsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }
}
