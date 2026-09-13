namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Register;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class RegisterCommandHandlerTests
{
    private static RegisterCommand Command(string email = "new@zari.local", string role = "Staff") =>
        new("New", "User", email, "Passw0rd!", "Passw0rd!", role);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Email_Already_Taken()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        await userManager.CreateAsync(new ApplicationUser { UserName = "new@zari.local", Email = "new@zari.local", FirstName = "x", LastName = "y" }, "Passw0rd!");
        var handler = new RegisterCommandHandler(userManager, roleManager);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.EmailTaken");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Role_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var handler = new RegisterCommandHandler(userManager, roleManager);

        var result = await handler.HandleAsync(Command(role: "GhostRole"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Register_And_Assign_Role()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        await roleManager.CreateAsync(new IdentityRole("Staff"));
        var handler = new RegisterCommandHandler(userManager, roleManager);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var user = await userManager.FindByEmailAsync("new@zari.local");
        user.Should().NotBeNull();
        (await userManager.GetRolesAsync(user!)).Should().Contain("Staff");
    }
}
