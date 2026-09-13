namespace ZARI.Application.UnitTests.Features.Hr.EmployeeScheduleAssignment;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Create;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Delete;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Get;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.GetAll;
using ZARI.Application.Features.Hr.EmployeeScheduleAssignments.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class ScheduleAssignmentTestHelpers
{
    public static async Task<(Guid EmployeeId, Guid WorkScheduleId)> SeedMasterData(AppDbContext db)
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (employee.Id, schedule.Id);
    }
}

public sealed class CreateEmployeeScheduleAssignmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Assignment()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var handler = new CreateEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateEmployeeScheduleAssignmentCommand(employeeId, scheduleId, DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEE_SCHEDULES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateEmployeeScheduleAssignmentCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new CreateEmployeeScheduleAssignmentCommand(employeeId, scheduleId, DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Close_Prior_Open_Assignment()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var priorAssignment = HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: scheduleId, effectiveDate: DateTimeOffset.UtcNow.AddDays(-60));
        db.EmployeeScheduleAssignments.Add(priorAssignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var newEffectiveDate = DateTimeOffset.UtcNow;
        var result = await handler.HandleAsync(new CreateEmployeeScheduleAssignmentCommand(employeeId, scheduleId, newEffectiveDate, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var closedPrior = await db.EmployeeScheduleAssignments.FindAsync([priorAssignment.Id], TestContext.Current.CancellationToken);
        closedPrior!.EndDate.Should().Be(newEffectiveDate.AddDays(-1));
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Employee_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateEmployeeScheduleAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class UpdateEmployeeScheduleAssignmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Assignment()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var assignment = HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: scheduleId);
        db.EmployeeScheduleAssignments.Add(assignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var endDate = DateTimeOffset.UtcNow;
        var result = await handler.HandleAsync(new UpdateEmployeeScheduleAssignmentCommand(assignment.Id, scheduleId, assignment.EffectiveDate, endDate), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.EmployeeScheduleAssignments.FindAsync([assignment.Id], TestContext.Current.CancellationToken);
        updated!.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateEmployeeScheduleAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteEmployeeScheduleAssignmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Assignment()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var assignment = HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: scheduleId);
        db.EmployeeScheduleAssignments.Add(assignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeScheduleAssignmentCommand(assignment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteEmployeeScheduleAssignmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeScheduleAssignmentCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetEmployeeScheduleAssignmentQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Assignment()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        var assignment = HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: scheduleId);
        db.EmployeeScheduleAssignments.Add(assignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetEmployeeScheduleAssignmentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeScheduleAssignmentQuery(assignment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetEmployeeScheduleAssignmentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeScheduleAssignmentQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllEmployeeScheduleAssignmentsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Assignments()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, scheduleId) = await ScheduleAssignmentTestHelpers.SeedMasterData(db);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employeeId, workScheduleId: scheduleId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllEmployeeScheduleAssignmentsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllEmployeeScheduleAssignmentsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
