namespace ZARI.Application.Features.Hr.LeaveRequests.Shared;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;

/// <summary>
/// Counts scheduled work days (per WorkDays on whichever EmployeeScheduleAssignment covers each
/// date) in [dateFrom, dateTo] inclusive — computed once at Create time and never recomputed
/// afterward (HrModuleContext.md §3.9: a later schedule change must not retroactively alter an
/// already-submitted request's day count). A date with no covering assignment is conservatively
/// counted as a leave day (there's no schedule to say otherwise), unlike DtrComputationService's
/// DTR-side convention of treating an uncovered date as "not applicable" — here the request still
/// needs a concrete day count to charge against the balance.
/// </summary>
public static class LeaveRequestDaysCalculator
{
    private static readonly Dictionary<DayOfWeek, string> DayCodes = new()
    {
        [DayOfWeek.Monday] = "MON",
        [DayOfWeek.Tuesday] = "TUE",
        [DayOfWeek.Wednesday] = "WED",
        [DayOfWeek.Thursday] = "THU",
        [DayOfWeek.Friday] = "FRI",
        [DayOfWeek.Saturday] = "SAT",
        [DayOfWeek.Sunday] = "SUN",
    };

    public static async Task<decimal> CalculateAsync(IAppDbContext dbContext, Guid employeeId, DateTimeOffset dateFrom, DateTimeOffset dateTo, CancellationToken cancellationToken)
    {
        var assignments = await dbContext.EmployeeScheduleAssignments
            .Where(a => a.EmployeeId == employeeId)
            .Select(a => new { a.EffectiveDate, a.EndDate, a.WorkScheduleId })
            .ToListAsync(cancellationToken);

        var scheduleIds = assignments.Select(a => a.WorkScheduleId).Distinct().ToList();
        var workDaysBySchedule = await dbContext.WorkSchedules
            .Where(w => scheduleIds.Contains(w.Id))
            .Select(w => new { w.Id, w.WorkDays })
            .ToDictionaryAsync(w => w.Id, w => w.WorkDays, cancellationToken);

        var count = 0m;
        for (var date = dateFrom.Date; date <= dateTo.Date; date = date.AddDays(1))
        {
            var assignment = assignments.FirstOrDefault(a => a.EffectiveDate.Date <= date && (a.EndDate is null || a.EndDate.Value.Date >= date));
            if (assignment is null || !workDaysBySchedule.TryGetValue(assignment.WorkScheduleId, out var workDays))
            {
                count += 1;
                continue;
            }

            var days = workDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (days.Contains(DayCodes[date.DayOfWeek]))
                count += 1;
        }

        return count;
    }
}
