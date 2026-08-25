namespace ZARI.Application.Features.Identity.Users.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteUserCommand(string Id) : ICommand;
