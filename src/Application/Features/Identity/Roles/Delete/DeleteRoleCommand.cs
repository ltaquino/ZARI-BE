namespace ZARI.Application.Features.Identity.Roles.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteRoleCommand(string Id) : ICommand;
