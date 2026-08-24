namespace ZARI.Application.Features.Identity.Login;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.DTOs.Identity;
using ZARI.Domain.Common;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<TokenResponse>>;
