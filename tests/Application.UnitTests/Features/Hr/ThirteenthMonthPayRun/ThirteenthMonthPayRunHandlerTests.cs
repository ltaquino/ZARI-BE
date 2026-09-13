namespace ZARI.Application.UnitTests.Features.Hr.ThirteenthMonthPayRun;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Approve;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Compute;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Create;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Delete;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.GetAll;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Reject;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Submit;
using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Update;
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

public static class ThirteenthMonthPayRunTestHelpers
{
    public static async Task<(AppDbContext Db, Employee Employee)> SeedBranchAndEmployeeAsync()
    {
        var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        var employee = HrTestFixtures.Employee(branchId: "br-1", positionId: position.Id, departmentId: department.Id);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, employee);
    }

    public static void SeedThirteenthMonthPayGlAccounts(AppDbContext db)
    {
        foreach (var code in new[] { "6026", "2340" })
            db.GlAccounts.Add(LoanTestFixtures.GlAccount(code: code));
    }

    /// <summary>Seeds a POSTED PayrollRun/PayrollLine so ComputeForEmployeeAsync has something to sum.</summary>
    public static async Task<PayrollRun> SeedPostedPayrollAsync(AppDbContext db, Guid employeeId, decimal basicPay, int year, string branchId = "br-1")
    {
        var run = HrTestFixtures.PayrollRun(payrollRunNo: $"HQ-PAY-{Guid.NewGuid():N}", branchId: branchId, status: "POSTED",
            payPeriodStart: new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero), payPeriodEnd: new DateTimeOffset(year, 1, 15, 0, 0, 0, TimeSpan.Zero));
        db.PayrollRuns.Add(run);
        db.PayrollLines.Add(HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employeeId, basicPay: basicPay));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return run;
    }
}

public sealed class CreateThirteenthMonthPayRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateThirteenthMonthPayRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-13P-000001"))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateThirteenthMonthPayRunCommand("br-1", 2026, new DateTimeOffset(2026, 12, 15, 0, 0, 0, TimeSpan.Zero)), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value!.Year.Should().Be(2026);
        result.Value!.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateThirteenthMonthPayRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-13P-000001"))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateThirteenthMonthPayRunCommand("br-missing", 2026, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionOnBranchAsync("13TH_MONTH_PAY_RUNS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateThirteenthMonthPayRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-13P-000001"))),
            permissions);

        var result = await handler.HandleAsync(new CreateThirteenthMonthPayRunCommand("br-1", 2026, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Run_Already_Exists_For_Branch_And_Year()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        db.ThirteenthMonthPayRuns.Add(HrTestFixtures.ThirteenthMonthPayRun(year: 2026));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateThirteenthMonthPayRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("HQ-13P-000002"))),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateThirteenthMonthPayRunCommand("br-1", 2026, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.Duplicate");
    }
}

public sealed class UpdateThirteenthMonthPayRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_PayDate_On_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun();
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var newPayDate = new DateTimeOffset(2027, 1, 5, 0, 0, 0, TimeSpan.Zero);

        var result = await handler.HandleAsync(new UpdateThirteenthMonthPayRunCommand(run.Id, newPayDate), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.ThirteenthMonthPayRuns.FindAsync([run.Id], TestContext.Current.CancellationToken);
        updated!.PayDate.Should().Be(newPayDate);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "POSTED");
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateThirteenthMonthPayRunCommand(run.Id, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotDraft");
    }
}

public sealed class DeleteThirteenthMonthPayRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Run()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun();
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "POSTED");
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotDraft");
    }
}

public sealed class GetThirteenthMonthPayRunQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Run_With_Lines()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.ThirteenthMonthPayRun();
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetThirteenthMonthPayRunQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetThirteenthMonthPayRunQuery(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetThirteenthMonthPayRunQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetThirteenthMonthPayRunQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllThirteenthMonthPayRunsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Runs()
    {
        await using var db = TestDbContextFactory.Create();
        db.ThirteenthMonthPayRuns.Add(HrTestFixtures.ThirteenthMonthPayRun(runNo: "HQ-13P-000001", year: 2025));
        db.ThirteenthMonthPayRuns.Add(HrTestFixtures.ThirteenthMonthPayRun(runNo: "HQ-13P-000002", year: 2026));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllThirteenthMonthPayRunsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllThirteenthMonthPayRunsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}

public sealed class ComputeThirteenthMonthPayRunCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Generate_Line_Summing_Posted_PayrollLines_For_The_Year()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000m, 2026);
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000m, 2026);
        var run = HrTestFixtures.ThirteenthMonthPayRun(year: 2026);
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputeThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().ContainSingle(l => l.EmployeeId == employee.Id && l.TotalBasicPay == 20000m && l.ThirteenthMonthPay == 1666.67m);
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_PayrollLines_From_Other_Years()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000m, 2025);
        var run = HrTestFixtures.ThirteenthMonthPayRun(year: 2026);
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputeThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Be_Safely_Rerunnable_And_Replace_Prior_Lines()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        await ThirteenthMonthPayRunTestHelpers.SeedPostedPayrollAsync(db, employee.Id, 10000m, 2026);
        var run = HrTestFixtures.ThirteenthMonthPayRun(year: 2026);
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputeThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        second.IsSuccess.Should().BeTrue();
        second.Value!.Lines.Should().HaveCount(1); // not doubled
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "PENDING_APPROVAL");
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ComputeThirteenthMonthPayRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ComputeThirteenthMonthPayRunCommand(run.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotDraft");
    }
}

public sealed class SubmitThirteenthMonthPayRunCommandHandlerTests
{
    private static SubmitThirteenthMonthPayRunCommandHandler BuildHandler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "PENDING", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "SUBMITTED", "APPROVAL_NEEDED", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun();
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitThirteenthMonthPayRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NoLines");
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_When_Draft_With_Lines()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.ThirteenthMonthPayRun();
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitThirteenthMonthPayRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "POSTED");
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new SubmitThirteenthMonthPayRunCommand(run.Id, "u1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotDraft");
    }
}

public sealed class ApproveThirteenthMonthPayRunCommandHandlerTests
{
    private static ApproveThirteenthMonthPayRunCommandHandler BuildHandler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Success(new GlJournalResponse(Guid.NewGuid(), "JV-0001", "br-1", DateTimeOffset.UtcNow, "HR", "ThirteenthMonthPayRuns", "x", null, "POSTED", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(AppDbContext Db, ThirteenthMonthPayRun Run)> SeedPendingRunAsync(bool withGlAccounts = true)
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "PENDING_APPROVAL");
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id, thirteenthMonthPay: 833.33m));
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "THIRTEENTH_MONTH_PAY_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        if (withGlAccounts)
            ThirteenthMonthPayRunTestHelpers.SeedThirteenthMonthPayGlAccounts(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, run);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_And_Post_When_Gl_Accounts_Configured()
    {
        var (db, run) = await SeedPendingRunAsync();
        await using var _ = db;
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveThirteenthMonthPayRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Missing()
    {
        var (db, run) = await SeedPendingRunAsync(withGlAccounts: false);
        await using var _ = db;
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveThirteenthMonthPayRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GlAccount.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "DRAFT");
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveThirteenthMonthPayRunCommand(run.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotPendingApproval");
    }
}

public sealed class RejectThirteenthMonthPayRunCommandHandlerTests
{
    private static RejectThirteenthMonthPayRunCommandHandler BuildHandler(AppDbContext db, IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "THIRTEENTH_MONTH_PAY_RUN", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        var (db, employee) = await ThirteenthMonthPayRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "PENDING_APPROVAL");
        db.ThirteenthMonthPayRuns.Add(run);
        db.ThirteenthMonthPayLines.Add(HrTestFixtures.ThirteenthMonthPayLine(thirteenthMonthPayRunId: run.Id, employeeId: employee.Id));
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "THIRTEENTH_MONTH_PAY_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectThirteenthMonthPayRunCommand(run.Id, "approver1", "needs fixing"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Approval()
    {
        await using var db = TestDbContextFactory.Create();
        var run = HrTestFixtures.ThirteenthMonthPayRun(status: "DRAFT");
        db.ThirteenthMonthPayRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectThirteenthMonthPayRunCommand(run.Id, "approver1", "needs fixing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ThirteenthMonthPayRun.NotPendingApproval");
    }
}
