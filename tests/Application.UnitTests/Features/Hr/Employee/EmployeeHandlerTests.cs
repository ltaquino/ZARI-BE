namespace ZARI.Application.UnitTests.Features.Hr.Employee;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.Employees.Create;
using ZARI.Application.Features.Hr.Employees.Delete;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Application.Features.Hr.Employees.GetAll;
using ZARI.Application.Features.Hr.Employees.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class EmployeeTestHelpers
{
    public static async Task<(string BranchId, Guid PositionId, Guid DepartmentId)> SeedMasterData(AppDbContext db)
    {
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (branch.Id, position.Id, department.Id);
    }
}

public sealed class CreateEmployeeCommandHandlerTests
{
    private static CreateEmployeeCommand Command(string branchId, Guid positionId, Guid departmentId, string employeeNo = "EMP-0001") =>
        new(employeeNo, "Juan", null, "Dela Cruz", null, null, null, null, null,
            branchId, positionId, departmentId, DateTimeOffset.UtcNow, "active", "PROBATIONARY",
            null, null, null, null, 20000m, null, null, null, null, null);

    [Fact]
    public async Task HandleAsync_Should_Create_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeNo.Should().Be("EMP-0001");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateEmployeeCommandHandler(db, permissions, IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_EmployeeNo()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0001", branchId: branchId, positionId: positionId, departmentId: departmentId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command("br-missing", position.Id, department.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_BasicSalary_Below_Position_PayGradeMin()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id, payGradeMin: 25000m, payGradeMax: 40000m);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branch.Id, position.Id, department.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.BelowPayGrade");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_BasicSalary_Above_Position_PayGradeMax()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id, payGradeMin: 5000m, payGradeMax: 10000m);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branch.Id, position.Id, department.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.AbovePayGrade");
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_BasicSalary_Within_Position_PayGrade()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id, payGradeMin: 15000m, payGradeMax: 25000m);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branch.Id, position.Id, department.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class UpdateEmployeeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var command = new UpdateEmployeeCommand(employee.Id, employee.EmployeeNo, "Juan", null, "Santos", null, null, null, null, null,
            branchId, positionId, departmentId, employee.DateHired, "active", "REGULAR", DateTimeOffset.UtcNow, null, null,
            null, null, null, null, 25000m, null, null, null, null, null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Employees.FindAsync([employee.Id], TestContext.Current.CancellationToken);
        updated!.LastName.Should().Be("Santos");
        updated.EmploymentStatus.Should().Be("REGULAR");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var command = new UpdateEmployeeCommand(Guid.NewGuid(), "EMP-0001", "Juan", null, "Santos", null, null, null, null, null,
            "br-1", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "active", "REGULAR", null, null, null,
            null, null, null, null, 25000m, null, null, null, null, null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_BasicSalary_Outside_Position_PayGrade()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id, payGradeMin: 15000m, payGradeMax: 25000m);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: branch.Id, positionId: position.Id, departmentId: department.Id, basicSalary: 20000m);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var command = new UpdateEmployeeCommand(employee.Id, employee.EmployeeNo, "Juan", null, "Santos", null, null, null, null, null,
            branch.Id, position.Id, department.Id, employee.DateHired, "active", "REGULAR", null, null, null,
            null, null, null, null, 30000m, null, null, null, null, null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.AbovePayGrade");
    }
}

public sealed class DeleteEmployeeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Is_Department_Head()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var department = await db.Departments.FindAsync([departmentId], TestContext.Current.CancellationToken);
        department!.HeadEmployeeId = employee.Id;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Leave_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        db.LeaveRequests.Add(HrTestFixtures.LeaveRequest(employeeId: employee.Id, leaveTypeId: leaveType.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.HasLeaveRequests");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Schedule_Assignments()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.HasScheduleAssignments");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Overtime_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.HasOvertimeRequests");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Allowances()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        db.EmployeeAllowances.Add(HrTestFixtures.EmployeeAllowance(employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.HasAllowances");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Deductions()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        db.EmployeeDeductions.Add(HrTestFixtures.EmployeeDeduction(employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Employee.HasDeductions");
    }
}

public sealed class GetEmployeeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetEmployeeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeQuery(employee.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeNo.Should().Be(employee.EmployeeNo);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetEmployeeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.View, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetEmployeeQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetEmployeeQuery(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllEmployeesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Employees()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0001", branchId: branchId, positionId: positionId, departmentId: departmentId));
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0002", branchId: branchId, positionId: positionId, departmentId: departmentId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllEmployeesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllEmployeesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EMPLOYEES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllEmployeesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllEmployeesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
