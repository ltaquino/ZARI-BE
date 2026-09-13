namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Application.Features.Identity.Users.Permissions.SetOverrides;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>The success path uses ExecuteDeleteAsync (unsupported by the InMemory provider, same
/// family as ExecuteUpdateAsync elsewhere in this sweep) — only guard clauses are exercised.</summary>
public sealed class SetUserPermissionOverridesCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new SetUserPermissionOverridesCommandHandler(IdentityTestFixtures.UserManager(db), db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SetUserPermissionOverridesCommand("missing-id", []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "u@zari.local", Email = "u@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SetUserPermissionOverridesCommandHandler(userManager, db, permissions);

        var result = await handler.HandleAsync(new SetUserPermissionOverridesCommand(user.Id, []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Form_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "u@zari.local", Email = "u@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var handler = new SetUserPermissionOverridesCommandHandler(userManager, db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(
            new SetUserPermissionOverridesCommand(user.Id, [new FormPermissionInput("GHOST_FORM", true, false, false, false, false, false)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Form.NotFound");
    }
}
