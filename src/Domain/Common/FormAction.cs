namespace ZARI.Domain.Common;

// Mirrors the six IFormPermissionFlags columns — the action a handler is asking IPermissionService
// to check the current user's effective permission for, on a given Form.
public enum FormAction
{
    View,
    Create,
    Edit,
    Approve,
    Cancel,
    Delete
}
