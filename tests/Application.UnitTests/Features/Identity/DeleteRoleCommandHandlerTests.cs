namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Roles.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteRoleCommandHandlerTests
{
    private static DeleteRoleCommandHandler Handler(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(roleManager, userManager, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(IdentityTestFixtures.RoleManager(db), IdentityTestFixtures.UserManager(db))
            .HandleAsync(new DeleteRoleCommand("missing-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ROLES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(roleManager, IdentityTestFixtures.UserManager(db), permissions)
            .HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Staff")]
    public async Task HandleAsync_Should_Fail_When_Deleting_System_Role(string systemRoleName)
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole(systemRoleName);
        await roleManager.CreateAsync(role);

        var result = await Handler(roleManager, IdentityTestFixtures.UserManager(db)).HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.SystemRole");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Role_Has_Assigned_Users()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "u@zari.local", Email = "u@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        await userManager.AddToRoleAsync(user, "Cashier");

        var result = await Handler(roleManager, userManager).HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.HasUsers");
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Unused_Custom_Role()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);

        var result = await Handler(roleManager, IdentityTestFixtures.UserManager(db)).HandleAsync(new DeleteRoleCommand(role.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await roleManager.FindByIdAsync(role.Id)).Should().BeNull();
    }
}
