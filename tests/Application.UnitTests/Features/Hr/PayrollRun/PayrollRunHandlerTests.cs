namespace ZARI.Application.UnitTests.Features.Hr.PayrollRun;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Hr.PayrollRuns.Approve;
using ZARI.Application.Features.Hr.PayrollRuns.Compute;
using ZARI.Application.Features.Hr.PayrollRuns.Create;
using ZARI.Application.Features.Hr.PayrollRuns.Delete;
using ZARI.Application.Features.Hr.PayrollRuns.Get;
using ZARI.Application.Features.Hr.PayrollRuns.GetAll;
using ZARI.Application.Features.Hr.PayrollRuns.Reject;
using ZARI.Application.Features.Hr.PayrollRuns.Submit;
using ZARI.Application.Features.Hr.PayrollRuns.Update;
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

public static class PayrollRunTestHelpers
{
    public static async Task<(AppDbContext Db, Employee Employee)> SeedBranchAndEmployeeAsync(decimal basicSalary = 20000m)
    {
        var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id, basicSalary: basicSalary,
            dateHired: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, employee);
    }

    public static void SeedPayrollGlAccounts(AppDbContext db)
    {
        foreach (var code in new[] { "6020", "6021", "6022", "6023", "2300", "2310", "2320", "2330", "2340", "2350" })
            db.GlAccounts.Add(LoanTestFixtures.GlAccount(code: code));
    }
}

public sealed class CreatePayrollRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-PAY-000001"))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreatePayrollRunCommand("br-1", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 16, 0, 0, 0, TimeSpan.Zero)), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-PAY-000001"))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreatePayrollRunCommand("br-missing", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PAYROLL_RUNS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-PAY-000001"))),
            permissions);

        var result = await handler.HandleAsync(new CreatePayrollRunCommand("br-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class UpdatePayrollRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var newStart = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var newEnd = new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var newPayDate = new DateTimeOffset(2026, 2, 16, 0, 0, 0, TimeSpan.Zero);

        var result = await handler.HandleAsync(new UpdatePayrollRunCommand(run.Id, newStart, newEnd, newPayDate), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.PayrollRuns.FindAsync([run.Id], TestContext.Current.CancellationToken);
        updated!.PayPeriodStart.Should().Be(newStart);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdatePayrollRunCommand(run.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotDraft");
    }
}

public sealed class DeletePayrollRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun(status: "POSTED");
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotDraft");
    }
}

public sealed class GetPayrollRunQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Run_With_Lines()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPayrollRunQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPayrollRunQuery(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPayrollRunQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPayrollRunQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllPayrollRunsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Runs()
    {
        await using var db = TestDbContextFactory.Create();
        db.PayrollRuns.Add(HrTestFixtures.PayrollRun(payrollRunNo: "HQ-PAY-000001"));
        db.PayrollRuns.Add(HrTestFixtures.PayrollRun(payrollRunNo: "HQ-PAY-000002"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPayrollRunsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPayrollRunsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}

public sealed class ComputePayrollRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Generate_One_Line_Per_Eligible_Employee()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync(basicSalary: 20000m);
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.EmployeeId == employee.Id && l.BasicPay == 10000.00m);
    }

    [Fact]
    public async Task HandleAsync_Should_Be_Safely_Rerunnable_And_Replace_Prior_Lines()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync(basicSalary: 20000m);
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        await handler.HandleAsync(new ComputePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(new ComputePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        second.IsSuccess.Should().BeTrue();
        second.Value!.Lines.Should().HaveCount(1); // not doubled
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Employees_From_Other_Branches()
    {
        var (db, _) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync(basicSalary: 20000m);
        await using var _db = db;
        db.Branches.Add(LoanTestFixtures.Branch(id: "br-2"));
        var otherDept = HrTestFixtures.Department(code: "DEPT-2");
        db.Departments.Add(otherDept);
        var otherPos = HrTestFixtures.Position(code: "POS-2", departmentId: otherDept.Id);
        db.Positions.Add(otherPos);
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0002", branchId: "br-2", positionId: otherPos.Id, departmentId: otherDept.Id));
        var run = HrTestFixtures.PayrollRun(branchId: "br-1");
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.Value!.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputePayrollRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotDraft");
    }
}

public sealed class SubmitPayrollRunCommandHandlerTests
{
    private static SubmitPayrollRunCommandHandler BuildHandler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "PENDING", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "SUBMITTED", "APPROVAL_NEEDED", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitPayrollRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NoLines");
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_When_Draft_With_Lines()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitPayrollRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun(status: "POSTED");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitPayrollRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotDraft");
    }
}

public sealed class ApprovePayrollRunCommandHandlerTests
{
    private static ApprovePayrollRunCommandHandler BuildHandler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success(new GlJournalResponse(Guid.NewGuid(), "JV-0001", "br-1", DateTimeOffset.UtcNow, "HR", "PayrollRun", "x", null, "POSTED", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(AppDbContext Db, ZARI.Domain.Entities.PayrollRun Run)> SeedPendingRunAsync(bool withGlAccounts = true)
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id, sssEmployeeShare: 250m, sssEmployerShare: 500m));
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "PAYROLL_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        if (withGlAccounts)
            PayrollRunTestHelpers.SeedPayrollGlAccounts(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, run);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_When_Gl_Accounts_Configured()
    {
        var (db, run) = await SeedPendingRunAsync();
        await using var _ = db;
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
    }

    [Fact]
    public async Task HandleAsync_Should_Post_Leave_Balance_Usage_When_A_Line_Consumed_Leave()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        var leaveType = HrTestFixtures.LeaveType(code: "VL", isPaid: true);
        db.LeaveTypes.Add(leaveType);
        await ZARI.Application.Features.Hr.LeaveBalanceEntries.Shared.LeaveBalanceLedgerService.PostEntryAsync(
            db, employee.Id, leaveType.Id, DateTimeOffset.UtcNow.AddDays(-10), "ACCRUAL", 5m, null, null, TestContext.Current.CancellationToken);
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id, leaveDaysConsumed: 1m, leaveTypeConsumedId: leaveType.Id));
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "PAYROLL_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        PayrollRunTestHelpers.SeedPayrollGlAccounts(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var usageEntry = db.LeaveBalanceEntries.Single(e => e.Type == "USAGE" && e.EmployeeId == employee.Id);
        usageEntry.Days.Should().Be(-1m);
        usageEntry.LeaveTypeId.Should().Be(leaveType.Id);
        usageEntry.ReferenceTable.Should().Be("PayrollRuns");
        usageEntry.ReferenceId.Should().Be(run.Id.ToString());
        usageEntry.RunningBalance.Should().Be(4m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Missing()
    {
        var (db, run) = await SeedPendingRunAsync(withGlAccounts: false);
        await using var _ = db;
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun(status: "DRAFT");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotPendingApproval");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
    }
}

public sealed class RejectPayrollRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id));
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "PAYROLL_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RejectPayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RejectPayrollRunCommand(run.Id, "approver1", "fix this"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.PayrollRun(status: "DRAFT");
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RejectPayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "PAYROLL_RUN", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new RejectPayrollRunCommand(run.Id, "approver1", "fix this"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PayrollRun.NotPendingApproval");
    }
}
