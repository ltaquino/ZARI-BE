namespace ZARI.Application.Features.SystemModule.DeviceSettings;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// The few company/branch facts an offline device (ZARI Mobile) needs but a cashier, encoder or driver can't read —
/// /api/company and /api/branches are admin screens: the discount approval threshold (so POS can tell before checkout
/// that a discount would need a manager) and the names of the user's own branches. Read-only and non-sensitive, so any
/// signed-in user may read it.
/// </summary>
public sealed record GetDeviceSettingsQuery : IQuery<Result<DeviceSettingsResponse>>;

public sealed record DeviceSettingsResponse(
    string CompanyName,
    string? TradeName,
    /// <summary>Company.MaxUnapprovedDiscountPct: a discretionary discount above this needs approval; null = no threshold.</summary>
    decimal? MaxUnapprovedDiscountPct,
    List<DeviceBranch> Branches);

public sealed record DeviceBranch(string Id, string Code, string Name, string Status);

public sealed class GetDeviceSettingsQueryHandler(IAppDbContext dbContext, ICurrentUser currentUser)
    : IQueryHandler<GetDeviceSettingsQuery, Result<DeviceSettingsResponse>>
{
    public async Task<Result<DeviceSettingsResponse>> HandleAsync(GetDeviceSettingsQuery query, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<DeviceSettingsResponse>(Error.Forbidden("DeviceSettings.NotSignedIn", "Sign in first."));

        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var assigned = await dbContext.UserBranches.Where(b => b.UserId == userId).Select(b => b.BranchId).ToListAsync(cancellationToken);
        // A user with no branch assignment (an administrator) works across all of them.
        var branches = await dbContext.Branches.AsNoTracking()
            .Where(b => assigned.Count == 0 ? b.Status == "active" : assigned.Contains(b.Id))
            .OrderBy(b => b.Name)
            .Select(b => new DeviceBranch(b.Id, b.Code, b.Name, b.Status))
            .ToListAsync(cancellationToken);

        return Result.Success(new DeviceSettingsResponse(company?.Name ?? "", company?.TradeName, company?.MaxUnapprovedDiscountPct, branches));
    }
}
