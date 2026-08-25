namespace ZARI.Application.Abstractions.Identity;

using ZARI.Domain.Common;

/// <summary>
/// Server-side enforcement of the effective-permission model built in the Users/Roles/Permissions
/// design (Form catalog x Role-template x per-user-override x per-action-flags, resolved the same
/// way as GetEffectiveUserPermissionsQuery — see EffectivePermissionResolver). The FE's
/// permissionGates.ts performs the equivalent check for UX only; this is what actually secures
/// the API.
/// </summary>
public interface IPermissionService
{
    /// <summary>Checks the current user's effective permission for a Form action with no branch dimension.</summary>
    Task<bool> HasPermissionAsync(string formCode, FormAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the current user's effective permission for a Form action AND that they're assigned
    /// to the given branch. Use for any entity that carries its own BranchId.
    /// </summary>
    Task<bool> HasPermissionOnBranchAsync(string formCode, FormAction action, string branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-branch cancellation-decision authority: CanCancel on the form AND assigned to the HQ
    /// branch — not scoped to the document's own branch, since the point is an HQ-level override.
    /// Mirrors the FE's permissionGates.ts canDecideCancellation. Use for the ApproveCancellation/
    /// RejectCancellation step of the two-tier cancellation flow on posted documents.
    /// </summary>
    Task<bool> HasCancellationAuthorityAsync(string formCode, CancellationToken cancellationToken = default);
}
