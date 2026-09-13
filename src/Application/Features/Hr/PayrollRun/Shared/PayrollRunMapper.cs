namespace ZARI.Application.Features.Hr.PayrollRuns.Shared;

using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Domain.Entities;

internal static class PayrollRunMapper
{
    public static PayrollRunResponse ToResponse(PayrollRun run) => new(
        run.Id,
        run.PayrollRunNo,
        run.BranchId,
        run.PayPeriodStart,
        run.PayPeriodEnd,
        run.PayDate,
        run.Status,
        run.CreatedAt,
        run.Lines.Select(ToLineResponse).ToList());

    private static PayrollLineResponse ToLineResponse(PayrollLine line) => new(
        line.Id,
        line.EmployeeId,
        line.Employee.EmployeeNo,
        $"{line.Employee.FirstName} {line.Employee.LastName}",
        line.BasicPay,
        line.OvertimePay,
        line.AllowanceTotal,
        line.LeaveDaysConsumed,
        line.LeaveTypeConsumedId,
        line.LeaveTypeConsumed?.Name,
        line.AbsenceDeduction,
        line.GrossPay,
        line.SssEmployeeShare,
        line.SssEmployerShare,
        line.PhilHealthEmployeeShare,
        line.PhilHealthEmployerShare,
        line.PagIbigEmployeeShare,
        line.PagIbigEmployerShare,
        line.WithholdingTax,
        line.DeductionTotal,
        line.NetPay);
}
