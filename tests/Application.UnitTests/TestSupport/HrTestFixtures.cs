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
        decimal basicSalary = 20000m) => new()
    {
        EmployeeNo = employeeNo,
        FirstName = firstName,
        LastName = lastName,
        BranchId = branchId,
        PositionId = positionId,
        DepartmentId = departmentId,
        DateHired = DateTimeOffset.UtcNow,
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
}
