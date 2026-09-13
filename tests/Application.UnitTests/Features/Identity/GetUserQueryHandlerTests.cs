namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Users.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetUserQueryHandlerTests
{
    private static GetUserQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string currentUserId = "someone-else",
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null)
    {
        var currentUser = Substitute.For<ZARI.Application.Abstractions.Identity.ICurrentUser>();
        currentUser.UserId.Returns(currentUserId);
        return new(userManager, roleManager, db, currentUser, permissions ?? LoanTestFixtures.AllowAllPermissionService());
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_And_Not_Self()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db), currentUserId: "someone-else", permissions: permissions)
            .HandleAsync(new GetUserQuery("target-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db))
            .HandleAsync(new GetUserQuery("missing-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Viewing_Self_Without_Users_Permission()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "me@zari.local", Email = "me@zari.local", FirstName = "Me", LastName = "User" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, userManager, IdentityTestFixtures.RoleManager(db), currentUserId: user.Id, permissions: permissions)
            .HandleAsync(new GetUserQuery(user.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("me@zari.local");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_User_With_Roles_And_Branches()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.UserBranches.Add(new UserBranch { UserId = "u1", BranchId = branch.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        await roleManager.CreateAsync(new IdentityRole("Staff"));
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { Id = "u1", UserName = "target@zari.local", Email = "target@zari.local", FirstName = "Target", LastName = "User" };
        await userManager.CreateAsync(user, "Passw0rd!");
        await userManager.AddToRoleAsync(user, "Staff");

        var result = await Handler(db, userManager, roleManager).HandleAsync(new GetUserQuery("u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RoleNames.Should().Contain("Staff");
        result.Value.BranchIds.Should().Contain(branch.Id);
    }
}
