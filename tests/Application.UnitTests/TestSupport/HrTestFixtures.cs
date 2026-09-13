namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

public static class HrTestFixtures
{
    public static Department Department(string code = "DEPT-1", string name = "Test Department", string status = "active", Guid? headEmployeeId = null) => new()
    {
        Code = code,
        Name = name,
        Status = status,
        HeadEmployeeId = headEmployeeId
    };

    public static Position Position(string code = "POS-1", string name = "Test Position", Guid departmentId = default, string status = "active") => new()
    {
        Code = code,
        Name = name,
        DepartmentId = departmentId,
        Status = status
    };

    public static Employee Employee(
        string employeeNo = "EMP-0001",
        string firstName = "Juan",
        string lastName = "Dela Cruz",
        string branchId = "br-1",
        Guid positionId = default,
        Guid departmentId = default,
        string status = "active",
        string employmentStatus = "PROBATIONARY",
        decimal basicSalary = 20000m,
        DateTimeOffset? dateHired = null) => new()
    {
        EmployeeNo = employeeNo,
        FirstName = firstName,
        LastName = lastName,
        BranchId = branchId,
        PositionId = positionId,
        DepartmentId = departmentId,
        DateHired = dateHired ?? DateTimeOffset.UtcNow,
        Status = status,
        EmploymentStatus = employmentStatus,
        BasicSalary = basicSalary
    };

    public static WorkSchedule WorkSchedule(
        string code = "SHIFT-1",
        string name = "Day Shift",
        string timeIn = "08:00",
        string timeOut = "17:00",
        string workDays = "MON,TUE,WED,THU,FRI",
        int gracePeriodMinutes = 10,
        int breakMinutes = 60,
        string status = "active") => new()
    {
        Code = code,
        Name = name,
        TimeIn = TimeOnly.Parse(timeIn),
        TimeOut = TimeOnly.Parse(timeOut),
        WorkDays = workDays,
        GracePeriodMinutes = gracePeriodMinutes,
        BreakMinutes = breakMinutes,
        Status = status
    };

    public static EmployeeScheduleAssignment EmployeeScheduleAssignment(
        Guid employeeId = default,
        Guid workScheduleId = default,
        DateTimeOffset? effectiveDate = null,
        DateTimeOffset? endDate = null) => new()
    {
        EmployeeId = employeeId,
        WorkScheduleId = workScheduleId,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow.AddDays(-30),
        EndDate = endDate
    };

    public static DailyTimeRecord DailyTimeRecord(
        Guid employeeId = default,
        DateTimeOffset? date = null,
        DateTimeOffset? timeIn = null,
        DateTimeOffset? timeOut = null,
        string source = "MANUAL") => new()
    {
        EmployeeId = employeeId,
        Date = date ?? DateTimeOffset.UtcNow.Date,
        TimeIn = timeIn,
        TimeOut = timeOut,
        Source = source
    };

    public static LeaveType LeaveType(
        string code = "VL",
        string name = "Vacation Leave",
        string accrualFrequency = "MONTHLY",
        decimal accrualAmount = 1.25m,
        decimal? maxCarryOverDays = null,
        bool isPaid = true,
        bool requiresDocumentation = false,
        string status = "active") => new()
    {
        Code = code,
        Name = name,
        AccrualFrequency = accrualFrequency,
        AccrualAmount = accrualAmount,
        MaxCarryOverDays = maxCarryOverDays,
        IsPaid = isPaid,
        RequiresDocumentation = requiresDocumentation,
        Status = status
    };

    public static LeaveRequest LeaveRequest(
        string requestNo = "HQ-LR-000001",
        Guid employeeId = default,
        Guid leaveTypeId = default,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        decimal days = 1m,
        string status = "DRAFT") => new()
    {
        RequestNo = requestNo,
        EmployeeId = employeeId,
        LeaveTypeId = leaveTypeId,
        DateFrom = dateFrom ?? DateTimeOffset.UtcNow,
        DateTo = dateTo ?? DateTimeOffset.UtcNow,
        Days = days,
        Status = status
    };

    public static OvertimeRateRule OvertimeRateRule(
        string otType = "ORDINARY",
        decimal multiplier = 1.25m,
        DateTimeOffset? effectiveDate = null,
        string status = "active") => new()
    {
        OtType = otType,
        Multiplier = multiplier,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        Status = status
    };

    public static OvertimeRequest OvertimeRequest(
        string requestNo = "HQ-OT-000001",
        Guid employeeId = default,
        DateTimeOffset? date = null,
        DateTimeOffset? plannedTimeFrom = null,
        DateTimeOffset? plannedTimeTo = null,
        string otType = "ORDINARY",
        string status = "DRAFT") => new()
    {
        RequestNo = requestNo,
        EmployeeId = employeeId,
        Date = date ?? DateTimeOffset.UtcNow,
        PlannedTimeFrom = plannedTimeFrom ?? DateTimeOffset.UtcNow,
        PlannedTimeTo = plannedTimeTo ?? DateTimeOffset.UtcNow.AddHours(2),
        OtType = otType,
        Status = status
    };

    public static EmployeeAllowance EmployeeAllowance(
        Guid employeeId = default,
        string code = "TRANSPO",
        string name = "Transportation Allowance",
        decimal amount = 1000m,
        DateTimeOffset? effectiveDate = null,
        DateTimeOffset? endDate = null,
        string status = "active") => new()
    {
        EmployeeId = employeeId,
        Code = code,
        Name = name,
        Amount = amount,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        EndDate = endDate,
        Status = status
    };

    public static EmployeeDeduction EmployeeDeduction(
        Guid employeeId = default,
        string code = "CASH-ADV",
        string name = "Cash Advance",
        decimal amount = 500m,
        DateTimeOffset? effectiveDate = null,
        DateTimeOffset? endDate = null,
        string status = "active") => new()
    {
        EmployeeId = employeeId,
        Code = code,
        Name = name,
        Amount = amount,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        EndDate = endDate,
        Status = status
    };

    public static SssContributionBracket SssContributionBracket(
        decimal salaryFrom = 0m,
        decimal salaryTo = 10000m,
        decimal employeeShare = 200m,
        decimal employerShare = 400m,
        DateTimeOffset? effectiveDate = null,
        string status = "active") => new()
    {
        SalaryFrom = salaryFrom,
        SalaryTo = salaryTo,
        EmployeeShare = employeeShare,
        EmployerShare = employerShare,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        Status = status
    };

    public static PagIbigContributionBracket PagIbigContributionBracket(
        decimal salaryFrom = 0m,
        decimal salaryTo = 100000m,
        decimal employeeShare = 200m,
        decimal employerShare = 400m,
        DateTimeOffset? effectiveDate = null,
        string status = "active") => new()
    {
        SalaryFrom = salaryFrom,
        SalaryTo = salaryTo,
        EmployeeShare = employeeShare,
        EmployerShare = employerShare,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        Status = status
    };

    public static GovernmentContributionRate GovernmentContributionRate(
        string contributionType = "PHILHEALTH",
        decimal salaryFrom = 0m,
        decimal salaryTo = 100000m,
        decimal employeeSharePct = 2m,
        decimal employerSharePct = 2m,
        decimal? maxContribution = null,
        DateTimeOffset? effectiveDate = null,
        string status = "active") => new()
    {
        ContributionType = contributionType,
        SalaryFrom = salaryFrom,
        SalaryTo = salaryTo,
        EmployeeSharePct = employeeSharePct,
        EmployerSharePct = employerSharePct,
        MaxContribution = maxContribution,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        Status = status
    };

    public static WithholdingTaxBracket WithholdingTaxBracket(
        decimal salaryFrom = 0m,
        decimal salaryTo = 20000m,
        decimal baseTax = 0m,
        decimal rateOverExcess = 0m,
        DateTimeOffset? effectiveDate = null,
        string status = "active") => new()
    {
        SalaryFrom = salaryFrom,
        SalaryTo = salaryTo,
        BaseTax = baseTax,
        RateOverExcess = rateOverExcess,
        EffectiveDate = effectiveDate ?? DateTimeOffset.UtcNow,
        Status = status
    };

    public static PayrollRun PayrollRun(
        string payrollRunNo = "HQ-PAY-000001",
        string branchId = "br-1",
        DateTimeOffset? payPeriodStart = null,
        DateTimeOffset? payPeriodEnd = null,
        DateTimeOffset? payDate = null,
        string status = "DRAFT") => new()
    {
        PayrollRunNo = payrollRunNo,
        BranchId = branchId,
        PayPeriodStart = payPeriodStart ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        PayPeriodEnd = payPeriodEnd ?? new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
        PayDate = payDate ?? new DateTimeOffset(2026, 1, 16, 0, 0, 0, TimeSpan.Zero),
        Status = status
    };

    public static PayrollLine PayrollLine(
        Guid payrollRunId = default,
        Guid employeeId = default,
        decimal basicPay = 10000m,
        decimal overtimePay = 0m,
        decimal allowanceTotal = 0m,
        decimal grossPay = 10000m,
        decimal sssEmployeeShare = 0m,
        decimal sssEmployerShare = 0m,
        decimal philHealthEmployeeShare = 0m,
        decimal philHealthEmployerShare = 0m,
        decimal pagIbigEmployeeShare = 0m,
        decimal pagIbigEmployerShare = 0m,
        decimal withholdingTax = 0m,
        decimal deductionTotal = 0m,
        decimal netPay = 10000m) => new()
    {
        PayrollRunId = payrollRunId,
        EmployeeId = employeeId,
        BasicPay = basicPay,
        OvertimePay = overtimePay,
        AllowanceTotal = allowanceTotal,
        GrossPay = grossPay,
        SssEmployeeShare = sssEmployeeShare,
        SssEmployerShare = sssEmployerShare,
        PhilHealthEmployeeShare = philHealthEmployeeShare,
        PhilHealthEmployerShare = philHealthEmployerShare,
        PagIbigEmployeeShare = pagIbigEmployeeShare,
        PagIbigEmployerShare = pagIbigEmployerShare,
        WithholdingTax = withholdingTax,
        DeductionTotal = deductionTotal,
        NetPay = netPay
    };
}
