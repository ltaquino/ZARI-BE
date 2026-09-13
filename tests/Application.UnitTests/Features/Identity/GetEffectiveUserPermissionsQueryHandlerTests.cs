namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Users.Permissions.GetEffective;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetEffectiveUserPermissionsQueryHandlerTests
{
    private static GetEffectiveUserPermissionsQueryHandler Handler(
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

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db), permissions: permissions)
            .HandleAsync(new GetEffectiveUserPermissionsQuery("target-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db))
            .HandleAsync(new GetEffectiveUserPermissionsQuery("missing-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Override_Beat_Role_Grant()
    {
        await using var db = TestDbContextFactory.Create();
        var form = SystemModuleTestFixtures.Form(code: "BRANCHES");
        db.Forms.Add(form);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Staff");
        await roleManager.CreateAsync(role);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, FormCode = "BRANCHES", CanView = true, CanEdit = true });
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { Id = "u1", UserName = "u@zari.local", Email = "u@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        await userManager.AddToRoleAsync(user, "Staff");
        db.UserFormPermissionOverrides.Add(new UserFormPermissionOverride { UserId = "u1", FormCode = "BRANCHES", CanView = true, CanEdit = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db, userManager, roleManager).HandleAsync(new GetEffectiveUserPermissionsQuery("u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var branchPerm = result.Value!.Should().ContainSingle(p => p.FormCode == "BRANCHES").Subject;
        branchPerm.IsOverridden.Should().BeTrue();
        branchPerm.CanEdit.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_Or_Across_Roles_When_No_Override()
    {
        await using var db = TestDbContextFactory.Create();
        var form = SystemModuleTestFixtures.Form(code: "BRANCHES");
        db.Forms.Add(form);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var roleA = new IdentityRole("RoleA");
        var roleB = new IdentityRole("RoleB");
        await roleManager.CreateAsync(roleA);
        await roleManager.CreateAsync(roleB);
        db.RolePermissions.Add(new RolePermission { RoleId = roleA.Id, FormCode = "BRANCHES", CanView = true, CanEdit = false });
        db.RolePermissions.Add(new RolePermission { RoleId = roleB.Id, FormCode = "BRANCHES", CanView = false, CanEdit = true });
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { Id = "u1", UserName = "u@zari.local", Email = "u@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        await userManager.AddToRoleAsync(user, "RoleA");
        await userManager.AddToRoleAsync(user, "RoleB");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db, userManager, roleManager).HandleAsync(new GetEffectiveUserPermissionsQuery("u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var branchPerm = result.Value!.Should().ContainSingle(p => p.FormCode == "BRANCHES").Subject;
        branchPerm.IsOverridden.Should().BeFalse();
        branchPerm.CanView.Should().BeTrue();
        branchPerm.CanEdit.Should().BeTrue();
    }
}
