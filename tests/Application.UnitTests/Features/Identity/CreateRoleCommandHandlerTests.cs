namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Roles.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateRoleCommandHandlerTests
{
    private static CreateRoleCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(IdentityTestFixtures.RoleManager(db), db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ROLES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new CreateRoleCommand("Cashier", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Name()
    {
        await using var db = TestDbContextFactory.Create();
        var roleManager = IdentityTestFixtures.RoleManager(db);
        await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Cashier"));
        var handler = new CreateRoleCommandHandler(roleManager, db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateRoleCommand("Cashier", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Role.DuplicateName");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Form_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(
            new CreateRoleCommand("Cashier", [new FormPermissionInput("GHOST_FORM", true, false, false, false, false, false)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Form.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Role_With_Permissions()
    {
        await using var db = TestDbContextFactory.Create();
        var form = SystemModuleTestFixtures.Form(code: "BRANCHES");
        db.Forms.Add(form);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(
            new CreateRoleCommand("Cashier", [new FormPermissionInput("BRANCHES", true, false, false, false, false, false)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Cashier");
        result.Value.Permissions.Should().ContainSingle(p => p.FormCode == "BRANCHES" && p.CanView);
    }
}
