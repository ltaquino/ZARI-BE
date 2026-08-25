namespace ZARI.Domain.Entities;

// One row per admin/transactional page. Seeded and read-only via the API — the set of forms is
// defined by what the app actually has pages for, not something an admin creates. Role templates
// and per-user overrides grant Form-level action flags against this catalog (see RolePermission,
// UserFormPermissionOverride).
public sealed class Form
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Module { get; set; } = default!;
}
