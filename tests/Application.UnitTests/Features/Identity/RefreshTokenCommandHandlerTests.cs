namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.DTOs.Identity;
using ZARI.Application.Features.Identity.RefreshToken;
using ZARI.Domain.Common;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Token_Service_Refreshes()
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.RefreshTokenAsync("old-access", "old-refresh", Arg.Any<CancellationToken>())
            .Returns(new TokenResponse { Id = "u1", UserName = "test@zari.local" });
        var handler = new RefreshTokenCommandHandler(tokenService);

        var result = await handler.HandleAsync(new RefreshTokenCommand("old-access", "old-refresh"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("test@zari.local");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Token_Service_Throws()
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<TokenResponse>(_ => throw new InvalidOperationException("Invalid refresh token."));
        var handler = new RefreshTokenCommandHandler(tokenService);

        var result = await handler.HandleAsync(new RefreshTokenCommand("bad-access", "bad-refresh"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.InvalidRefreshToken");
    }
}
