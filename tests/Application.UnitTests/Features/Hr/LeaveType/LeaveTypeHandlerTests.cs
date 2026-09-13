namespace ZARI.Application.UnitTests.Features.Hr.LeaveType;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.LeaveTypes.Create;
using ZARI.Application.Features.Hr.LeaveTypes.Delete;
using ZARI.Application.Features.Hr.LeaveTypes.Get;
using ZARI.Application.Features.Hr.LeaveTypes.GetAll;
using ZARI.Application.Features.Hr.LeaveTypes.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateLeaveTypeCommandHandlerTests
{
    private static CreateLeaveTypeCommand Command(string code = "VL") =>
        new(code, "Vacation Leave", "MONTHLY", 1.25m, null, true, false, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_LeaveType()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("VL");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LEAVE_TYPES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateLeaveTypeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Code()
    {
        await using var db = TestDbContextFactory.Create();
        db.LeaveTypes.Add(HrTestFixtures.LeaveType(code: "VL"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateLeaveTypeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_LeaveType()
    {
        await using var db = TestDbContextFactory.Create();
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var command = new UpdateLeaveTypeCommand(leaveType.Id, "VL2", "Vacation Leave v2", "ANNUAL", 15m, 5m, true, false, "active");
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.LeaveTypes.FindAsync([leaveType.Id], TestContext.Current.CancellationToken);
        updated!.AccrualFrequency.Should().Be("ANNUAL");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateLeaveTypeCommand(Guid.NewGuid(), "VL", "Vacation Leave", "MONTHLY", 1.25m, null, true, false, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteLeaveTypeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_LeaveType()
    {
        await using var db = TestDbContextFactory.Create();
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLeaveTypeCommand(leaveType.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        db.LeaveRequests.Add(HrTestFixtures.LeaveRequest(employeeId: employee.Id, leaveTypeId: leaveType.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteLeaveTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLeaveTypeCommand(leaveType.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetLeaveTypeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_LeaveType()
    {
        await using var db = TestDbContextFactory.Create();
        var leaveType = HrTestFixtures.LeaveType();
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveTypeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLeaveTypeQuery(leaveType.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(leaveType.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLeaveTypeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLeaveTypeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllLeaveTypesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_LeaveTypes()
    {
        await using var db = TestDbContextFactory.Create();
        db.LeaveTypes.Add(HrTestFixtures.LeaveType(code: "VL"));
        db.LeaveTypes.Add(HrTestFixtures.LeaveType(code: "SL"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLeaveTypesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLeaveTypesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
