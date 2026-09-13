namespace ZARI.Application.UnitTests.Features.Hr.LeaveRequest;

using ZARI.Application.Features.Hr.LeaveRequests.Shared;
using ZARI.Application.UnitTests.TestSupport;

public sealed class LeaveRequestDaysCalculatorTests
{
    // A Monday, so a MON-FRI schedule always covers it regardless of which week the test runs in.
    private static readonly DateTimeOffset AMonday = new(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CalculateAsync_Should_Count_Only_Work_Days_In_Range()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule(workDays: "MON,TUE,WED,THU,FRI");
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: AMonday.AddDays(-30)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Monday through the following Monday (8 calendar days) = 6 work days (2 weekends excluded).
        var days = await LeaveRequestDaysCalculator.CalculateAsync(db, employee.Id, AMonday, AMonday.AddDays(7), TestContext.Current.CancellationToken);

        days.Should().Be(6);
    }

    [Fact]
    public async Task CalculateAsync_Should_Count_Single_Day_As_One()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule(workDays: "MON,TUE,WED,THU,FRI");
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: AMonday.AddDays(-30)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var days = await LeaveRequestDaysCalculator.CalculateAsync(db, employee.Id, AMonday, AMonday, TestContext.Current.CancellationToken);

        days.Should().Be(1);
    }

    [Fact]
    public async Task CalculateAsync_Should_Count_Uncovered_Dates_As_Leave_Days()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // No schedule assignment at all -> conservatively counts every date as a leave day.
        var days = await LeaveRequestDaysCalculator.CalculateAsync(db, employee.Id, AMonday, AMonday.AddDays(2), TestContext.Current.CancellationToken);

        days.Should().Be(3);
    }
}
