namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Users.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateUserCommandHandlerTests
{
    private static CreateUserCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(userManager, roleManager, db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static CreateUserCommand Command(string email, List<string>? roleIds = null, List<string>? branchIds = null) =>
        new(email, "New", "User", "555-1234", "active", "Passw0rd!", roleIds ?? [], branchIds ?? []);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db), permissions)
            .HandleAsync(Command("new@zari.local"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Email_Already_Taken()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        await userManager.CreateAsync(new ApplicationUser { UserName = "new@zari.local", Email = "new@zari.local", FirstName = "x", LastName = "y" }, "Passw0rd!");

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db)).HandleAsync(Command("new@zari.local"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.EmailTaken");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Role_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command("new@zari.local", roleIds: [Guid.NewGuid().ToString()]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command("new@zari.local", branchIds: ["br-missing"]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_User_With_Roles_And_Branches()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Staff");
        await roleManager.CreateAsync(role);
        var userManager = IdentityTestFixtures.UserManager(db);

        var result = await Handler(db, userManager, roleManager)
            .HandleAsync(Command("new@zari.local", roleIds: [role.Id], branchIds: [branch.Id]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RoleNames.Should().Contain("Staff");
        result.Value.BranchIds.Should().Contain(branch.Id);
        var created = await userManager.FindByEmailAsync("new@zari.local");
        created.Should().NotBeNull();
        (await userManager.CheckPasswordAsync(created!, "Passw0rd!")).Should().BeTrue();
    }
}
