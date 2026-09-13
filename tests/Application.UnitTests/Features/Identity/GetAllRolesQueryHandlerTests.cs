namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Roles.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllRolesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ROLES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllRolesQueryHandler(IdentityTestFixtures.RoleManager(db), db, permissions);

        var result = await handler.HandleAsync(new GetAllRolesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Roles_Ordered_By_Name_With_Permissions()
    {
        await using var db = TestDbContextFactory.Create();
        var form = SystemModuleTestFixtures.Form(code: "BRANCHES");
        db.Forms.Add(form);
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var zeta = new IdentityRole("Zeta");
        await roleManager.CreateAsync(zeta);
        var alpha = new IdentityRole("Alpha");
        await roleManager.CreateAsync(alpha);
        db.RolePermissions.Add(new RolePermission { RoleId = alpha.Id, FormCode = "BRANCHES", CanView = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllRolesQueryHandler(roleManager, db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllRolesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Alpha");
        result.Value![0].Permissions.Should().ContainSingle(p => p.FormCode == "BRANCHES");
        result.Value![1].Name.Should().Be("Zeta");
    }
}
