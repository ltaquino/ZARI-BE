namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.DTOs.Identity;
using ZARI.Application.Features.Identity.Login;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class LoginCommandHandlerTests
{
    private static ZARI.Application.Abstractions.Identity.ITokenService FakeTokenService()
    {
        var service = Substitute.For<ZARI.Application.Abstractions.Identity.ITokenService>();
        service.GenerateTokenAsync(Arg.Any<ApplicationUser>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResponse { Id = "u1", UserName = "test@zari.local" });
        return service;
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_User_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new LoginCommandHandler(IdentityTestFixtures.UserManager(db), FakeTokenService());

        var result = await handler.HandleAsync(new LoginCommand("nobody@zari.local", "Whatever1!"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Password_Wrong()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "test@zari.local", Email = "test@zari.local", FirstName = "Test", LastName = "User" };
        await userManager.CreateAsync(user, "CorrectPass1!");
        var handler = new LoginCommandHandler(userManager, FakeTokenService());

        var result = await handler.HandleAsync(new LoginCommand("test@zari.local", "WrongPass1!"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_With_Correct_Credentials()
    {
        await using var db = TestDbContextFactory.Create();
        var userManager = IdentityTestFixtures.UserManager(db);
        var user = new ApplicationUser { UserName = "test@zari.local", Email = "test@zari.local", FirstName = "Test", LastName = "User" };
        await userManager.CreateAsync(user, "CorrectPass1!");
        var handler = new LoginCommandHandler(userManager, FakeTokenService());

        var result = await handler.HandleAsync(new LoginCommand("test@zari.local", "CorrectPass1!"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("test@zari.local");
    }
}
