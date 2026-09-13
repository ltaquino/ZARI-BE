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
}
