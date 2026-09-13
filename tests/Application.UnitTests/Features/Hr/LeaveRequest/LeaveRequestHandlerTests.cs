namespace ZARI.Application.UnitTests.Features.Hr.LeaveRequest;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.LeaveRequests.Approve;
using ZARI.Application.Features.Hr.LeaveRequests.Create;
using ZARI.Application.Features.Hr.LeaveRequests.Delete;
using ZARI.Application.Features.Hr.LeaveRequests.Get;
using ZARI.Application.Features.Hr.LeaveRequests.GetAll;
using ZARI.Application.Features.Hr.LeaveRequests.Reject;
using ZARI.Application.Features.Hr.LeaveRequests.Submit;
using ZARI.Application.Features.Hr.LeaveRequests.Update;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

public static class LeaveRequestTestHelpers
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

public sealed class CreateLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Request_With_Computed_Days()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var handler = new CreateLeaveRequestCommandHandler(db, new GetNextDocumentNumberCommandHandlerStub(), LoanTestFixtures.AllowAllPermissionService());

        var dateFrom = DateTimeOffset.UtcNow;
        var result = await handler.HandleAsync(new CreateLeaveRequestCommand(employeeId, leaveTypeId, dateFrom, dateFrom, "Personal", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value.Days.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LEAVE_REQUESTS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateLeaveRequestCommandHandler(db, new GetNextDocumentNumberCommandHandlerStub(), permissions);

        var result = await handler.HandleAsync(new CreateLeaveRequestCommand(employeeId, leaveTypeId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_LeaveType_Does_Not_Exist()
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
        var handler = new CreateLeaveRequestCommandHandler(db, new GetNextDocumentNumberCommandHandlerStub(), LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateLeaveRequestCommand(employee.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

/// <summary>A minimal real stand-in (not a mock) so Create tests don't depend on DocumentSequence seed rows.</summary>
public sealed class GetNextDocumentNumberCommandHandlerStub :
    ZARI.Application.Abstractions.Messaging.ICommandHandler<
        ZARI.Application.Features.SystemModule.DocumentSequences.GetNext.GetNextDocumentNumberCommand,
        Result<ZARI.Application.Features.SystemModule.DocumentSequences.GetNext.NextDocumentNumberResponse>>
{
    private int _counter = 1;

    public Task<Result<ZARI.Application.Features.SystemModule.DocumentSequences.GetNext.NextDocumentNumberResponse>> HandleAsync(
        ZARI.Application.Features.SystemModule.DocumentSequences.GetNext.GetNextDocumentNumberCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new ZARI.Application.Features.SystemModule.DocumentSequences.GetNext.NextDocumentNumberResponse($"HQ-LR-{_counter++:D6}")));
}

public sealed class UpdateLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLeaveRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateLeaveRequestCommand(request.Id, leaveTypeId, request.DateFrom, request.DateTo, "Updated reason", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.LeaveRequests.FindAsync([request.Id], TestContext.Current.CancellationToken);
        updated!.Reason.Should().Be("Updated reason");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "PENDING_APPROVAL");
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLeaveRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateLeaveRequestCommand(request.Id, leaveTypeId, request.DateFrom, request.DateTo, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LeaveRequest.NotDraft");
    }
}

public sealed class DeleteLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteLeaveRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLeaveRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "APPROVED");
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteLeaveRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLeaveRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LeaveRequest.NotDraft");
    }
}

public sealed class GetLeaveRequestQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLeaveRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequestNo.Should().Be(request.RequestNo);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLeaveRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLeaveRequestQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllLeaveRequestsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        db.LeaveRequests.Add(HrTestFixtures.LeaveRequest(requestNo: "HQ-LR-000001", employeeId: employeeId, leaveTypeId: leaveTypeId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLeaveRequestsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLeaveRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}

public sealed class SubmitLeaveRequestCommandHandlerTests
{
    private static SubmitLeaveRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Submit_Draft_For_Approval()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new SubmitLeaveRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "LEAVE_REQUEST" && r.EntityId == request.Id.ToString());
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "APPROVED");
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new SubmitLeaveRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LeaveRequest.NotDraft");
    }
}

public sealed class ApproveLeaveRequestCommandHandlerTests
{
    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "LEAVE_REQUEST", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static ApproveLeaveRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(AppDbContext Db, LeaveRequest Request)> Seed(bool withApprovalRequest = true)
    {
        var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "PENDING_APPROVAL", days: 2m);
        db.LeaveRequests.Add(request);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LEAVE_REQUEST", EntityId = request.Id.ToString(), BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_Usage_Ledger_Entry()
    {
        var (db, request) = await Seed();

        var result = await Handler(db).HandleAsync(new ApproveLeaveRequestCommand(request.Id, "approver", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("APPROVED");
        var ledgerEntry = db.LeaveBalanceEntries.Should().ContainSingle(e => e.ReferenceTable == "LeaveRequests" && e.ReferenceId == request.Id.ToString()).Subject;
        ledgerEntry.Type.Should().Be("USAGE");
        ledgerEntry.Days.Should().Be(-2m);
        ledgerEntry.RunningBalance.Should().Be(-2m);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        var (db, request) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new ApproveLeaveRequestCommand(request.Id, "approver", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "DRAFT");
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveLeaveRequestCommand(request.Id, "approver", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LeaveRequest.NotPendingApproval");
        await db.DisposeAsync();
    }
}

public sealed class RejectLeaveRequestCommandHandlerTests
{
    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "LEAVE_REQUEST", "x", "br-1", "user", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static RejectLeaveRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var (employeeId, leaveTypeId) = await LeaveRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.LeaveRequest(employeeId: employeeId, leaveTypeId: leaveTypeId, status: "PENDING_APPROVAL");
        db.LeaveRequests.Add(request);
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LEAVE_REQUEST", EntityId = request.Id.ToString(), BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RejectLeaveRequestCommand(request.Id, "approver", "Please revise"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        db.LeaveBalanceEntries.Should().BeEmpty();
    }
}
