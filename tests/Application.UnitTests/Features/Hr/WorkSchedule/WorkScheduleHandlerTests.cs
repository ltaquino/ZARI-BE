namespace ZARI.Application.UnitTests.Features.Hr.WorkSchedule;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.WorkSchedules.Create;
using ZARI.Application.Features.Hr.WorkSchedules.Delete;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Application.Features.Hr.WorkSchedules.GetAll;
using ZARI.Application.Features.Hr.WorkSchedules.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateWorkScheduleCommandHandlerTests
{
    private static CreateWorkScheduleCommand Command(string code = "SHIFT-1") =>
        new(code, "Day Shift", new TimeOnly(8, 0), new TimeOnly(17, 0), "MON,TUE,WED,THU,FRI", 10, 60, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_WorkSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SHIFT-1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("WORK_SCHEDULES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateWorkScheduleCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Code()
    {
        await using var db = TestDbContextFactory.Create();
        db.WorkSchedules.Add(HrTestFixtures.WorkSchedule(code: "SHIFT-1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateWorkScheduleCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_WorkSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var command = new UpdateWorkScheduleCommand(schedule.Id, "SHIFT-2", "Night Shift", new TimeOnly(20, 0), new TimeOnly(5, 0), "MON,TUE,WED,THU,FRI", 5, 60, "active");
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.WorkSchedules.FindAsync([schedule.Id], TestContext.Current.CancellationToken);
        updated!.Name.Should().Be("Night Shift");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var command = new UpdateWorkScheduleCommand(Guid.NewGuid(), "SHIFT-1", "Day Shift", new TimeOnly(8, 0), new TimeOnly(17, 0), "MON,TUE,WED,THU,FRI", 10, 60, "active");
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteWorkScheduleCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_WorkSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWorkScheduleCommand(schedule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Assignments()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWorkScheduleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWorkScheduleCommand(schedule.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetWorkScheduleQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_WorkSchedule()
    {
        await using var db = TestDbContextFactory.Create();
        var schedule = HrTestFixtures.WorkSchedule();
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetWorkScheduleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWorkScheduleQuery(schedule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(schedule.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetWorkScheduleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWorkScheduleQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllWorkSchedulesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_WorkSchedules()
    {
        await using var db = TestDbContextFactory.Create();
        db.WorkSchedules.Add(HrTestFixtures.WorkSchedule(code: "S1"));
        db.WorkSchedules.Add(HrTestFixtures.WorkSchedule(code: "S2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllWorkSchedulesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllWorkSchedulesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
