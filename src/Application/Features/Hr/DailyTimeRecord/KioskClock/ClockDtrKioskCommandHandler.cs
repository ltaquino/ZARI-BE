namespace ZARI.Application.Features.Hr.DailyTimeRecords.KioskClock;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The DTR kiosk clock-in/out endpoint (HrModuleContext.md §3.6) — resolves an employee by
/// BarcodeNo or RfidTagNo, with no individual employee login involved. Reuses POS Mode's "one
/// authenticated station, many non-authenticated transacting parties" shape verbatim: the
/// permission check below is evaluated against whoever/whatever is logged into the kiosk station
/// itself (an HR staff account, or a dedicated low-privilege "Kiosk" role holding only
/// DAILY_TIME_RECORDS Create/Edit on its branch via the existing Users/Roles system — no new
/// permission plumbing needed), never against the scanned employee, exactly like POS checkout
/// checks the cashier's permission, not the customer's. Confirmed via direct research: no
/// alternate "terminal identity" pathway exists anywhere in IPermissionService/CurrentUser — every
/// endpoint in this codebase, POS included, sits behind the same JWT-bearer .RequireAuthorization(),
/// so this one does too.
///
/// One row per employee per day, same as the manual entry path — first scan of the day writes
/// TimeIn (or, on the rare row that already exists with TimeIn null, e.g. a manual encoder
/// pre-created a TimeOut-only row, still treated as the TIME_IN event), second scan writes TimeOut.
/// A third scan on an already-completed day is rejected rather than silently overwriting history.
/// </summary>
public sealed class ClockDtrKioskCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<ClockDtrKioskCommand, Result<DtrKioskClockResponse>>
{
    public async Task<Result<DtrKioskClockResponse>> HandleAsync(ClockDtrKioskCommand command, CancellationToken cancellationToken = default)
    {
        var scannedCode = command.ScannedCode.Trim();

        var employee = await dbContext.Employees.FirstOrDefaultAsync(
            e => e.Status == "active" && (e.BarcodeNo == scannedCode || e.RfidTagNo == scannedCode),
            cancellationToken);
        if (employee is null)
            return Result.Failure<DtrKioskClockResponse>(Error.NotFound("Employee.NotFound", "No active employee found with that badge."));

        var now = DateTimeOffset.UtcNow;
        // DateTimeOffset.Date returns a plain DateTime with the offset stripped — assigning it
        // straight to a DateTimeOffset (record.Date below) or comparing it against one implicitly
        // reinterprets it in the machine's LOCAL time zone rather than UTC. Rebuild explicitly, the
        // same fix already applied once this session in PayrollComputationService.
        var today = new DateTimeOffset(now.Date, TimeSpan.Zero);

        var record = await dbContext.DailyTimeRecords
            .FirstOrDefaultAsync(d => d.EmployeeId == employee.Id && d.Date.Date == today.Date, cancellationToken);

        string clockAction;
        if (record is null)
        {
            if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Create, employee.BranchId, cancellationToken))
                return Result.Failure<DtrKioskClockResponse>(Error.Forbidden("DailyTimeRecord.Forbidden", "This station does not have permission to record time for this branch."));

            record = new DailyTimeRecord
            {
                EmployeeId = employee.Id,
                Date = today,
                TimeIn = now,
                Source = "KIOSK"
            };
            dbContext.DailyTimeRecords.Add(record);
            clockAction = "TIME_IN";
        }
        else if (record.TimeIn is null)
        {
            if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Edit, employee.BranchId, cancellationToken))
                return Result.Failure<DtrKioskClockResponse>(Error.Forbidden("DailyTimeRecord.Forbidden", "This station does not have permission to record time for this branch."));

            record.TimeIn = now;
            clockAction = "TIME_IN";
        }
        else if (record.TimeOut is null)
        {
            if (!await permissionService.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Edit, employee.BranchId, cancellationToken))
                return Result.Failure<DtrKioskClockResponse>(Error.Forbidden("DailyTimeRecord.Forbidden", "This station does not have permission to record time for this branch."));

            record.TimeOut = now;
            clockAction = "TIME_OUT";
        }
        else
        {
            return Result.Failure<DtrKioskClockResponse>(Error.Conflict("DailyTimeRecord.AlreadyClockedOut", "You have already completed your time in/out for today."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var computed = await DtrComputationService.ComputeAsync(dbContext, record.EmployeeId, record.Date, record.TimeIn, record.TimeOut, cancellationToken);
        var response = new DtrKioskClockResponse(
            employee.Id, employee.EmployeeNo, $"{employee.FirstName} {employee.LastName}",
            clockAction, now, record.Id, computed.LateMinutes, computed.UndertimeMinutes);
        return Result.Success(response);
    }
}
