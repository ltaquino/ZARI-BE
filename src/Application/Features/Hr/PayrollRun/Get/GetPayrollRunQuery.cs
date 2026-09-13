namespace ZARI.Application.Features.Hr.PayrollRuns.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetPayrollRunQuery(Guid Id) : IQuery<Result<PayrollRunResponse>>;

public sealed record PayrollLineResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    decimal BasicPay,
    decimal OvertimePay,
    decimal AllowanceTotal,
    decimal LeaveDaysConsumed,
    Guid? LeaveTypeConsumedId,
    string? LeaveTypeConsumedName,
    decimal AbsenceDeduction,
    decimal GrossPay,
    decimal SssEmployeeShare,
    decimal SssEmployerShare,
    decimal PhilHealthEmployeeShare,
    decimal PhilHealthEmployerShare,
    decimal PagIbigEmployeeShare,
    decimal PagIbigEmployerShare,
    decimal WithholdingTax,
    decimal DeductionTotal,
    decimal NetPay);

public sealed record PayrollRunResponse(
    Guid Id,
    string PayrollRunNo,
    string BranchId,
    DateTimeOffset PayPeriodStart,
    DateTimeOffset PayPeriodEnd,
    DateTimeOffset PayDate,
    string Status,
    DateTimeOffset CreatedAt,
    List<PayrollLineResponse> Lines);
