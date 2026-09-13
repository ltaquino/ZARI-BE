namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.Features.Identity.Users.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteUserCommandHandler(IdentityTestFixtures.UserManager(db), LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteUserCommand("missing-id"), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("USERS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteUserCommandHandler(userManager, permissions);

        var result = await handler.HandleAsync(new DeleteUserCommand(user.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Existing_User()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "existing@zari.local", Email = "existing@zari.local", FirstName = "x", LastName = "y" };
        await userManager.CreateAsync(user, "Passw0rd!");
        var handler = new DeleteUserCommandHandler(userManager, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteUserCommand(user.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await userManager.FindByIdAsync(user.Id)).Should().BeNull();
    }
}
