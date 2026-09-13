namespace ZARI.Application.UnitTests.Features.Hr.OvertimeRequest;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.OvertimeRequests.Approve;
using ZARI.Application.Features.Hr.OvertimeRequests.Create;
using ZARI.Application.Features.Hr.OvertimeRequests.Delete;
using ZARI.Application.Features.Hr.OvertimeRequests.Get;
using ZARI.Application.Features.Hr.OvertimeRequests.GetAll;
using ZARI.Application.Features.Hr.OvertimeRequests.Reject;
using ZARI.Application.Features.Hr.OvertimeRequests.Submit;
using ZARI.Application.Features.Hr.OvertimeRequests.Update;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

public static class OvertimeRequestTestHelpers
{
    public static async Task<Guid> SeedMasterData(AppDbContext db)
    {
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return employee.Id;
    }
}

public sealed class GetNextOtDocumentNumberStub :
    ZARI.Application.Abstractions.Messaging.ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>
{
    private int _counter = 1;

    public Task<Result<NextDocumentNumberResponse>> HandleAsync(GetNextDocumentNumberCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new NextDocumentNumberResponse($"HQ-OT-{_counter++:D6}")));
}

public sealed class CreateOvertimeRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var handler = new CreateOvertimeRequestCommandHandler(db, new GetNextOtDocumentNumberStub(), LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateOvertimeRequestCommand(employeeId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), "ORDINARY", "Month-end closing"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("OVERTIME_REQUESTS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateOvertimeRequestCommandHandler(db, new GetNextOtDocumentNumberStub(), permissions);

        var result = await handler.HandleAsync(new CreateOvertimeRequestCommand(employeeId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), "ORDINARY", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Employee_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateOvertimeRequestCommandHandler(db, new GetNextOtDocumentNumberStub(), LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateOvertimeRequestCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), "ORDINARY", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class UpdateOvertimeRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId);
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateOvertimeRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateOvertimeRequestCommand(request.Id, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, "HOLIDAY", "Updated"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.OvertimeRequests.FindAsync([request.Id], TestContext.Current.CancellationToken);
        updated!.OtType.Should().Be("HOLIDAY");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "APPROVED");
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateOvertimeRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateOvertimeRequestCommand(request.Id, request.Date, request.PlannedTimeFrom, request.PlannedTimeTo, "HOLIDAY", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OvertimeRequest.NotDraft");
    }
}

public sealed class DeleteOvertimeRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId);
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOvertimeRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteOvertimeRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "APPROVED");
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOvertimeRequestCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteOvertimeRequestCommand(request.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OvertimeRequest.NotDraft");
    }
}

public sealed class GetOvertimeRequestQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId);
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOvertimeRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetOvertimeRequestQuery(request.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequestNo.Should().Be(request.RequestNo);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetOvertimeRequestQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetOvertimeRequestQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllOvertimeRequestsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Requests()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        db.OvertimeRequests.Add(HrTestFixtures.OvertimeRequest(requestNo: "HQ-OT-000001", employeeId: employeeId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllOvertimeRequestsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllOvertimeRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}

public sealed class SubmitOvertimeRequestCommandHandlerTests
{
    private static SubmitOvertimeRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Submit_Draft_For_Approval()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId);
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new SubmitOvertimeRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "OVERTIME_REQUEST" && r.EntityId == request.Id.ToString());
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "APPROVED");
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new SubmitOvertimeRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OvertimeRequest.NotDraft");
    }
}

public sealed class ApproveOvertimeRequestCommandHandlerTests
{
    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "OVERTIME_REQUEST", "x", "br-1", "user", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static ApproveOvertimeRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Approve_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "PENDING_APPROVAL");
        db.OvertimeRequests.Add(request);
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "OVERTIME_REQUEST", EntityId = request.Id.ToString(), BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveOvertimeRequestCommand(request.Id, "approver", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("APPROVED");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Approval_Request_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "PENDING_APPROVAL");
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new ApproveOvertimeRequestCommand(request.Id, "approver", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class RejectOvertimeRequestCommandHandlerTests
{
    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "OVERTIME_REQUEST", "x", "br-1", "user", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static RejectOvertimeRequestCommandHandler Handler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db, LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await OvertimeRequestTestHelpers.SeedMasterData(db);
        var request = HrTestFixtures.OvertimeRequest(employeeId: employeeId, status: "PENDING_APPROVAL");
        db.OvertimeRequests.Add(request);
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "OVERTIME_REQUEST", EntityId = request.Id.ToString(), BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new RejectOvertimeRequestCommand(request.Id, "approver", "Not needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
    }
}
