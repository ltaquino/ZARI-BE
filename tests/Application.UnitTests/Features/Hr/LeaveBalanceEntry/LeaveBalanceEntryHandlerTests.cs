namespace ZARI.Application.UnitTests.Features.Hr.LeaveBalanceEntry;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.CreateAdjustment;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.GetAll;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class LeaveBalanceTestHelpers
{
    public static async Task<(Guid EmployeeId, Guid LeaveTypeId)> SeedMasterData(AppDbContext db)
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (employee.Id, leaveType.Id);
    }
}

public sealed class LeaveBalanceLedgerServiceTests
{
    [Fact]
    public async Task PostEntryAsync_Should_Start_Running_Balance_From_Zero()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveBalanceTestHelpers.SeedMasterData(db);

        var entry = await LeaveBalanceLedgerService.PostEntryAsync(db, employeeId, leaveTypeId, DateTimeOffset.UtcNow, "ACCRUAL", 1.25m, null, null, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        entry.RunningBalance.Should().Be(1.25m);
    }

    [Fact]
    public async Task PostEntryAsync_Should_Accumulate_Onto_Prior_Balance()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveBalanceTestHelpers.SeedMasterData(db);
        await LeaveBalanceLedgerService.PostEntryAsync(db, employeeId, leaveTypeId, DateTimeOffset.UtcNow.AddDays(-30), "ACCRUAL", 1.25m, null, null, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var entry = await LeaveBalanceLedgerService.PostEntryAsync(db, employeeId, leaveTypeId, DateTimeOffset.UtcNow, "USAGE", -1m, "LeaveRequests", "x", TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        entry.RunningBalance.Should().Be(0.25m);
    }
}

public sealed class CreateLeaveBalanceAdjustmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Post_Entry()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveBalanceTestHelpers.SeedMasterData(db);
        var handler = new CreateLeaveBalanceAdjustmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateLeaveBalanceAdjustmentCommand(employeeId, leaveTypeId, DateTimeOffset.UtcNow, "ACCRUAL", 1.25m, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RunningBalance.Should().Be(1.25m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveBalanceTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LEAVE_BALANCE_ENTRIES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateLeaveBalanceAdjustmentCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new CreateLeaveBalanceAdjustmentCommand(employeeId, leaveTypeId, DateTimeOffset.UtcNow, "ACCRUAL", 1.25m, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetLeaveBalanceEntryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLeaveBalanceEntryQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLeaveBalanceEntryQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllLeaveBalanceEntriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Entries()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveBalanceTestHelpers.SeedMasterData(db);
        await LeaveBalanceLedgerService.PostEntryAsync(db, employeeId, leaveTypeId, DateTimeOffset.UtcNow, "ACCRUAL", 1.25m, null, null, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLeaveBalanceEntriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLeaveBalanceEntriesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
