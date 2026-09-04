namespace ZARI.Application.Features.Reporting.ReportTemplates.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Reporting.Datasets;

/// <summary>
/// Report Designer's branch-access enforcement — mirrors how every branch-scoped WRITE elsewhere in
/// this app already works (UserBranch assignment, checked via IPermissionService.HasPermissionOnBranchAsync),
/// extended to reads: any dataset that carries a "BranchId" field (the FK-naming convention every
/// dataset already uses) has its results — and its filter-value suggestions — always restricted to
/// the current user's own assigned branches, regardless of what filter the report itself specifies.
/// Since Admin is normally assigned to every branch already, this changes nothing for Admin; it
/// only actually restricts a Manager/Staff user scoped to fewer branches.
///
/// Implemented as ONE extra filter appended to whatever filters a query already has — reusing the
/// exact same Text/In filter mechanism (<see cref="ReportDatasetFilters.Text"/>) every dataset with
/// a BranchId field already implements for its own filtering — so this needs ZERO changes to any
/// individual dataset file (BranchesReportDataset is the one exception: it gained its own BranchId
/// field, backed by the branch's own Id, specifically so this same generic mechanism also scopes
/// the Branches listing itself — see that file's doc comment).
/// </summary>
public static class ReportBranchScope
{
    private const string BranchFieldKey = "BranchId";

    /// <summary>Returns the extra filter(s) to append to a dataset run — empty if the dataset has no
    /// BranchId field at all (nothing to scope). If the current user has zero branch assignments,
    /// returns a filter that can never match any real branch — fail closed, not open, rather than
    /// silently showing everything.</summary>
    public static async Task<List<ReportFilterValue>> BuildAsync(
        IAppDbContext dbContext,
        ICurrentUser currentUser,
        IReportDataset dataset,
        CancellationToken cancellationToken)
    {
        if (!dataset.Fields.Any(f => f.Key == BranchFieldKey))
            return [];

        var userBranchIds = await dbContext.UserBranches
            .Where(ub => ub.UserId == currentUser.UserId)
            .Select(ub => ub.BranchId)
            .ToListAsync(cancellationToken);

        var value = userBranchIds.Count == 0
            ? "__NO_BRANCH_ASSIGNED__" // matches no real Branch.Id — fails closed
            : string.Join(",", userBranchIds);

        return [new ReportFilterValue(BranchFieldKey, ReportFilterOperator.In, value)];
    }
}
