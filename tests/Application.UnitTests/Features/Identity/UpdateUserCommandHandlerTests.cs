namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Users.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The full success path (role sync + ExecuteDeleteAsync on UserBranches) is not InMemory-testable
/// — ExecuteDeleteAsync is unsupported by the InMemory provider, same family as ExecuteUpdateAsync
/// elsewhere in this sweep. Only the guard clauses before that point are exercised.
/// </summary>
public sealed class UpdateUserCommandHandlerTests
{
    private static UpdateUserCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(userManager, roleManager, db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateUserCommand Command(string id, string email = "existing@zari.local", List<string>? roleIds = null, List<string>? branchIds = null) =>
        new(id, email, "Updated", "User", null, "active", roleIds ?? [], branchIds ?? []);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command("missing-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "existing@zari.local", Email = "existing@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db), permissions)
            .HandleAsync(Command(user.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_New_Email_Taken_By_Another_User()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "existing@zari.local", Email = "existing@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var other = new ApplicationUser { UserName = "other@zari.local", Email = "other@zari.local", FirstName = "a", LastName = "b" };
        await userManager.CreateAsync(other, "Passw0rd!");

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command(user.Id, email: "other@zari.local"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.EmailTaken");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Role_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "existing@zari.local", Email = "existing@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command(user.Id, roleIds: [Guid.NewGuid().ToString()]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "existing@zari.local", Email = "existing@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db))
            .HandleAsync(Command(user.Id, branchIds: ["br-missing"]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}
