namespace ZARI.Application.UnitTests.Features.Hr.DailyTimeRecord;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.DailyTimeRecords.KioskClock;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

public static class KioskClockTestHelpers
{
    public static async Task<Employee> SeedEmployeeAsync(AppDbContext db, string? barcodeNo = "BC-0001", string? rfidTagNo = null, string status = "active")
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id, status: status);
        employee.BarcodeNo = barcodeNo;
        employee.RfidTagNo = rfidTagNo;
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return employee;
    }
}

public sealed class ClockDtrKioskCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Clock_In_When_No_Record_Exists_Yet()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ClockAction.Should().Be("TIME_IN");
        result.Value!.EmployeeId.Should().Be(employee.Id);

        var record = await db.DailyTimeRecords.FindAsync([result.Value!.DailyTimeRecordId], TestContext.Current.CancellationToken);
        record!.TimeIn.Should().NotBeNull();
        record.TimeOut.Should().BeNull();
        record.Source.Should().Be("KIOSK");
    }

    [Fact]
    public async Task HandleAsync_Should_Match_By_RfidTagNo_Too()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db, barcodeNo: null, rfidTagNo: "RFID-9999");
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("RFID-9999"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ClockAction.Should().Be("TIME_IN");
    }

    [Fact]
    public async Task HandleAsync_Should_Trim_Whitespace_From_Scanned_Code()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("  BC-0001\r\n"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Clock_Out_On_Second_Scan_Of_The_Day()
    {
        await using var db = TestDbContextFactory.Create();
        var employee = await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ClockAction.Should().Be("TIME_OUT");
        var record = db.DailyTimeRecords.Single(d => d.EmployeeId == employee.Id);
        record.TimeIn.Should().NotBeNull();
        record.TimeOut.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_On_Third_Scan_Of_An_Already_Completed_Day()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DailyTimeRecord.AlreadyClockedOut");
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Employee_Matches_The_Scanned_Code()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("NO-SUCH-BADGE"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Employee_Is_Inactive()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db, status: "inactive");
        var handler = new ClockDtrKioskCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Station_Lacks_Create_Permission()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ClockDtrKioskCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Station_Lacks_Edit_Permission_On_Second_Scan()
    {
        await using var db = TestDbContextFactory.Create();
        await KioskClockTestHelpers.SeedEmployeeAsync(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        var handler = new ClockDtrKioskCommandHandler(db, permissions);
        await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);
        permissions.HasPermissionOnBranchAsync("DAILY_TIME_RECORDS", FormAction.Edit, "br-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.HandleAsync(new ClockDtrKioskCommand("BC-0001"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
