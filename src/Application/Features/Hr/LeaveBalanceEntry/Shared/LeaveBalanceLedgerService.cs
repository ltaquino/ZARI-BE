namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Domain.Entities;

/// <summary>
/// Appends one row to the LeaveBalanceEntry ledger, computing RunningBalance from whatever the
/// employee/leave-type pair's last posted entry was — same append-only idiom as LoanLedgerEntry.
/// Callers pass Days already signed correctly (negative for USAGE) — this service doesn't flip
/// the sign itself, since ADJUSTMENT can legitimately go either way.
/// </summary>
public static class LeaveBalanceLedgerService
{
    public static async Task<LeaveBalanceEntry> PostEntryAsync(
        IAppDbContext dbContext, Guid employeeId, Guid leaveTypeId, DateTimeOffset date, string type, decimal days,
        string? referenceTable, string? referenceId, CancellationToken cancellationToken)
    {
        var lastBalance = await dbContext.LeaveBalanceEntries
            .Where(e => e.EmployeeId == employeeId && e.LeaveTypeId == leaveTypeId)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt)
            .Select(e => (decimal?)e.RunningBalance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var entry = new LeaveBalanceEntry
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Date = date,
            Type = type,
            Days = days,
            RunningBalance = lastBalance + days,
            ReferenceTable = referenceTable,
            ReferenceId = referenceId
        };

        dbContext.LeaveBalanceEntries.Add(entry);
        return entry;
    }
}
