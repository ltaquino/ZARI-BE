namespace ZARI.Application.Features.Hr.DailyTimeRecords.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;

/// <summary>
/// Derives late/undertime/absent at read time against whichever EmployeeScheduleAssignment was
/// effective on a given date — never stored on DailyTimeRecord itself (see
/// HrModuleContext.md §3.6: a later schedule correction must not leave a stale computed value
/// behind). Late = minutes clocked in after the schedule's TimeIn + GracePeriodMinutes. Undertime
/// = minutes clocked out before the schedule's TimeOut. Absent = a scheduled work day with no
/// TimeIn recorded at all. A day the employee isn't scheduled to work (per WorkDays, or no
/// assignment covers it) computes as "not applicable" (all null, not absent) rather than absent.
/// </summary>
public static class DtrComputationService
{
    public sealed record DtrComputedFields(int? LateMinutes, int? UndertimeMinutes, bool IsAbsent);

    public static async Task<DtrComputedFields> ComputeAsync(
        IAppDbContext dbContext, Guid employeeId, DateTimeOffset date, DateTimeOffset? timeIn, DateTimeOffset? timeOut, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.EmployeeScheduleAssignments
            .Where(a => a.EmployeeId == employeeId && a.EffectiveDate <= date && (a.EndDate == null || a.EndDate >= date))
            .Select(a => new { a.WorkScheduleId })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
            return new DtrComputedFields(null, null, false);

        var schedule = await dbContext.WorkSchedules
            .Where(w => w.Id == assignment.WorkScheduleId)
            .Select(w => new { w.TimeIn, w.TimeOut, w.WorkDays, w.GracePeriodMinutes })
            .FirstOrDefaultAsync(cancellationToken);

        if (schedule is null)
            return new DtrComputedFields(null, null, false);

        var workDays = schedule.WorkDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dayCode = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "MON",
            DayOfWeek.Tuesday => "TUE",
            DayOfWeek.Wednesday => "WED",
            DayOfWeek.Thursday => "THU",
            DayOfWeek.Friday => "FRI",
            DayOfWeek.Saturday => "SAT",
            _ => "SUN"
        };
        if (!workDays.Contains(dayCode))
            return new DtrComputedFields(null, null, false);

        if (timeIn is null)
            return new DtrComputedFields(null, null, true);

        var scheduledIn = schedule.TimeIn.AddMinutes(schedule.GracePeriodMinutes);
        var actualIn = TimeOnly.FromTimeSpan(timeIn.Value.TimeOfDay);
        var lateMinutes = Math.Max(0, (int)(actualIn.ToTimeSpan() - scheduledIn.ToTimeSpan()).TotalMinutes);

        int? undertimeMinutes = null;
        if (timeOut is not null)
        {
            var actualOut = TimeOnly.FromTimeSpan(timeOut.Value.TimeOfDay);
            undertimeMinutes = Math.Max(0, (int)(schedule.TimeOut.ToTimeSpan() - actualOut.ToTimeSpan()).TotalMinutes);
        }

        return new DtrComputedFields(lateMinutes, undertimeMinutes, false);
    }
}
