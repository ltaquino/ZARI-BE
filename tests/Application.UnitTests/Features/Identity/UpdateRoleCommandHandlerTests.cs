namespace ZARI.Application.UnitTests.Features.Identity;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Roles.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>The success path uses ExecuteDeleteAsync (unsupported by the InMemory provider, same
/// family as ExecuteUpdateAsync elsewhere in this sweep) — only guard clauses are exercised.</summary>
public sealed class UpdateRoleCommandHandlerTests
{
    private static UpdateRoleCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(roleManager, db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, IdentityTestFixtures.RoleManager(db))
            .HandleAsync(new UpdateRoleCommand("missing-id", "New Name", []), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("ROLES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, roleManager, permissions).HandleAsync(new UpdateRoleCommand(role.Id, "Renamed", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Staff")]
    public async Task HandleAsync_Should_Fail_When_Renaming_System_Role(string systemRoleName)
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole(systemRoleName);
        await roleManager.CreateAsync(role);

        var result = await Handler(db, roleManager).HandleAsync(new UpdateRoleCommand(role.Id, "Renamed", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.SystemRole");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_New_Name_Taken_By_Another_Role()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);
        var other = new IdentityRole("Supervisor");
        await roleManager.CreateAsync(other);

        var result = await Handler(db, roleManager).HandleAsync(new UpdateRoleCommand(role.Id, "Supervisor", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.DuplicateName");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Form_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        var role = new IdentityRole("Cashier");
        await roleManager.CreateAsync(role);

        var result = await Handler(db, roleManager).HandleAsync(
            new UpdateRoleCommand(role.Id, "Cashier", [new FormPermissionInput("GHOST_FORM", true, false, false, false, false, false)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Form.NotFound");
    }
}
