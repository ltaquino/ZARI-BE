namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Roles.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetRoleQueryHandlerTests
{
    private static GetRoleQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(roleManager, db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ROLES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, IdentityTestFixtures.RoleManager(db), permissions).HandleAsync(new GetRoleQuery("any-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.RoleManager(db)).HandleAsync(new GetRoleQuery("missing-id"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Role_With_Permissions()
    {
        await using var db = TestDbContextFactory.Create();
        var form = SystemModuleTestFixtures.Form(code: "BRANCHES");
        db.Forms.Add(form);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, FormCode = "BRANCHES", CanView = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db, roleManager).HandleAsync(new GetRoleQuery(role.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Cashier");
        result.Value.Permissions.Should().ContainSingle(p => p.FormCode == "BRANCHES" && p.CanView);
    }
}
