namespace ZARI.Application.Features.Identity.Users.Permissions.SetOverrides;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Identity.Permissions.Shared;

// Replace-all semantics: the list is the user's complete override set. A form omitted from the
// list has no override row and falls back to whatever its roles grant — this is how an override
// is "cleared" (an empty list reverts the user entirely to role-derived permissions).
public sealed record SetUserPermissionOverridesCommand(string UserId, List<FormPermissionInput> Overrides) : ICommand;
