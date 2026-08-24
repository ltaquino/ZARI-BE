using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.DTOs.Identity;
using ZARI.Domain.Common;

namespace ZARI.Application.Features.Identity.RefreshToken;

public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommand<Result<TokenResponse>>;
