namespace ZARI.Application.UnitTests.Features.Hr.DailyTimeRecord;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.DailyTimeRecords.Create;
using ZARI.Application.Features.Hr.DailyTimeRecords.Delete;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Application.Features.Hr.DailyTimeRecords.GetAll;
using ZARI.Application.Features.Hr.DailyTimeRecords.Shared;
using ZARI.Application.Features.Hr.DailyTimeRecords.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class DtrTestHelpers
{
    // A Monday, so WorkDays "MON,TUE,WED,THU,FRI" always covers it regardless of which week the
    // test happens to run in.
    public static readonly DateTimeOffset AMonday = new(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);

    public static async Task<(Guid EmployeeId, Guid WorkScheduleId)> SeedMasterDataWithAssignment(AppDbContext db)
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var schedule = HrTestFixtures.WorkSchedule(timeIn: "08:00", timeOut: "17:00", gracePeriodMinutes: 10);
        db.WorkSchedules.Add(schedule);
        db.EmployeeScheduleAssignments.Add(HrTestFixtures.EmployeeScheduleAssignment(employeeId: employee.Id, workScheduleId: schedule.Id, effectiveDate: AMonday.AddDays(-30)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (employee.Id, schedule.Id);
    }
}

public sealed class DtrComputationServiceTests
{
    [Fact]
    public async Task ComputeAsync_Should_Return_Zero_Late_When_On_Time()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var timeIn = DtrTestHelpers.AMonday.AddHours(8);

        var result = await DtrComputationService.ComputeAsync(db, employeeId, DtrTestHelpers.AMonday, timeIn, null, TestContext.Current.CancellationToken);

        result.LateMinutes.Should().Be(0);
        result.IsAbsent.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeAsync_Should_Compute_Late_Minutes_Beyond_Grace_Period()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        // Scheduled 08:00 + 10 min grace = 08:10; clocked in 08:25 -> 15 minutes late.
        var timeIn = DtrTestHelpers.AMonday.AddHours(8).AddMinutes(25);

        var result = await DtrComputationService.ComputeAsync(db, employeeId, DtrTestHelpers.AMonday, timeIn, null, TestContext.Current.CancellationToken);

        result.LateMinutes.Should().Be(15);
    }

    [Fact]
    public async Task ComputeAsync_Should_Compute_Undertime_Minutes()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var timeIn = DtrTestHelpers.AMonday.AddHours(8);
        var timeOut = DtrTestHelpers.AMonday.AddHours(16).AddMinutes(30);

        var result = await DtrComputationService.ComputeAsync(db, employeeId, DtrTestHelpers.AMonday, timeIn, timeOut, TestContext.Current.CancellationToken);

        result.UndertimeMinutes.Should().Be(30);
    }

    [Fact]
    public async Task ComputeAsync_Should_Be_Absent_When_No_TimeIn_On_A_Work_Day()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);

        var result = await DtrComputationService.ComputeAsync(db, employeeId, DtrTestHelpers.AMonday, null, null, TestContext.Current.CancellationToken);

        result.IsAbsent.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeAsync_Should_Not_Flag_Absent_On_A_Non_Work_Day()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var saturday = DtrTestHelpers.AMonday.AddDays(5);

        var result = await DtrComputationService.ComputeAsync(db, employeeId, saturday, null, null, TestContext.Current.CancellationToken);

        result.IsAbsent.Should().BeFalse();
        result.LateMinutes.Should().BeNull();
    }

    [Fact]
    public async Task ComputeAsync_Should_Return_Nulls_When_No_Schedule_Assignment_Covers_The_Date()
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

        var result = await DtrComputationService.ComputeAsync(db, employee.Id, DtrTestHelpers.AMonday, null, null, TestContext.Current.CancellationToken);

        result.LateMinutes.Should().BeNull();
        result.IsAbsent.Should().BeFalse();
    }
}

public sealed class CreateDailyTimeRecordCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Record()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var handler = new CreateDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var command = new CreateDailyTimeRecordCommand(employeeId, DtrTestHelpers.AMonday, DtrTestHelpers.AMonday.AddHours(8), null, null, null, "MANUAL", null);
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAbsent.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateDailyTimeRecordCommandHandler(db, permissions);

        var command = new CreateDailyTimeRecordCommand(employeeId, DtrTestHelpers.AMonday, null, null, null, null, "MANUAL", null);
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Record_Already_Exists_For_Date()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employeeId, date: DtrTestHelpers.AMonday));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var command = new CreateDailyTimeRecordCommand(employeeId, DtrTestHelpers.AMonday, null, null, null, null, "MANUAL", null);
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateDailyTimeRecordCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Record()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var record = HrTestFixtures.DailyTimeRecord(employeeId: employeeId, date: DtrTestHelpers.AMonday, timeIn: DtrTestHelpers.AMonday.AddHours(8));
        db.DailyTimeRecords.Add(record);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var timeOut = DtrTestHelpers.AMonday.AddHours(17);
        var result = await handler.HandleAsync(new UpdateDailyTimeRecordCommand(record.Id, record.TimeIn, timeOut, null, null, "MANUAL", "Regularized"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.DailyTimeRecords.FindAsync([record.Id], TestContext.Current.CancellationToken);
        updated!.TimeOut.Should().Be(timeOut);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateDailyTimeRecordCommand(Guid.NewGuid(), null, null, null, null, "MANUAL", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteDailyTimeRecordCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Record()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var record = HrTestFixtures.DailyTimeRecord(employeeId: employeeId, date: DtrTestHelpers.AMonday);
        db.DailyTimeRecords.Add(record);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDailyTimeRecordCommand(record.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteDailyTimeRecordCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDailyTimeRecordCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetDailyTimeRecordQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Record_With_Computed_Fields()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        var record = HrTestFixtures.DailyTimeRecord(employeeId: employeeId, date: DtrTestHelpers.AMonday, timeIn: DtrTestHelpers.AMonday.AddHours(8).AddMinutes(30));
        db.DailyTimeRecords.Add(record);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetDailyTimeRecordQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDailyTimeRecordQuery(record.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LateMinutes.Should().Be(20);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetDailyTimeRecordQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDailyTimeRecordQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllDailyTimeRecordsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Records()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, _) = await DtrTestHelpers.SeedMasterDataWithAssignment(db);
        db.DailyTimeRecords.Add(HrTestFixtures.DailyTimeRecord(employeeId: employeeId, date: DtrTestHelpers.AMonday));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllDailyTimeRecordsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllDailyTimeRecordsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
