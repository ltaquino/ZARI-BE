namespace ZARI.Application.Features.Identity.Users.ResetPassword;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Admin-side password reset -- see ResetUserPasswordCommandHandler's doc comment for why this
/// exists (there was previously NO path, self-service or admin, to recover a locked-out account).
/// </summary>
public sealed record ResetUserPasswordCommand(string UserId, string NewPassword) : ICommand;
