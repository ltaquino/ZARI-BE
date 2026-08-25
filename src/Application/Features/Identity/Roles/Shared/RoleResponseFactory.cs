namespace ZARI.Application.Features.Identity.Roles.Shared;

using Microsoft.AspNetCore.Identity;
using ZARI.Application.Features.Identity.Permissions.Shared;
using ZARI.Domain.Entities;

public static class RoleResponseFactory
{
    public static RoleResponse Build(IdentityRole role, List<RolePermission> rolePermissions, List<Form> forms)
    {
        var formByCode = forms.ToDictionary(f => f.Code);

        var permissions = rolePermissions
            .Where(rp => formByCode.ContainsKey(rp.FormCode))
            .Select(rp =>
            {
                var form = formByCode[rp.FormCode];
                return new FormPermissionResponse(form.Code, form.Name, form.Module, rp.CanView, rp.CanCreate, rp.CanEdit, rp.CanApprove, rp.CanCancel, rp.CanDelete);
            })
            .ToList();

        return new RoleResponse(role.Id, role.Name!, permissions);
    }
}
