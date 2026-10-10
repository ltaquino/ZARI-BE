namespace ZARI.Application.UnitTests.Features.Hr.PayrollRun;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Hr.EmployeeDeductions.Create;
using ZARI.Application.Features.Hr.EmployeeDeductions.Delete;
using ZARI.Application.Features.Hr.EmployeeDeductions.Update;
using ZARI.Application.Features.Hr.PayrollRuns.Approve;
using ZARI.Application.Features.Hr.PayrollRuns.Compute;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// A route-sales trip shortage charged through payroll was debited to 1220 Due from Employees at trip close,
/// so payroll must credit 1220 (clearing it) for that deduction — not 2350 like ordinary deductions.
/// </summary>
public sealed class PayrollReceivableRecoveryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Compute_Splits_Out_The_Trip_Shortage_Part_Of_The_Deductions()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun();
        db.PayrollRuns.Add(run);
        db.EmployeeDeductions.AddRange(
            HrTestFixtures.EmployeeDeduction(employee.Id, amount: 100m, effectiveDate: run.PayPeriodStart),
            HrTestFixtures.EmployeeDeduction(employee.Id, code: EmployeeDeduction.TripShortageCode, name: "Trip HQ-TRP-000001 shortage", amount: 32m,
                effectiveDate: run.PayPeriodStart, endDate: run.PayPeriodStart));
        await db.SaveChangesAsync(Ct);

        var result = await new ComputePayrollRunCommandHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new ComputePayrollRunCommand(run.Id), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var line = result.Value!.Lines.Single();
        line.DeductionTotal.Should().Be(132m, "net pay still nets out every deduction");
        line.ReceivableRecoveryDeduction.Should().Be(32m);
    }

    [Fact]
    public async Task Approve_Credits_1220_For_The_Trip_Shortage_And_2350_For_The_Rest()
    {
        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var run = HrTestFixtures.PayrollRun(status: "PENDING_APPROVAL");
        db.PayrollRuns.Add(run);
        var line = HrTestFixtures.PayrollLine(payrollRunId: run.Id, employeeId: employee.Id, deductionTotal: 132m, netPay: 9868m);
        line.ReceivableRecoveryDeduction = 32m;
        db.PayrollLines.Add(line);
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "PAYROLL_RUN", EntityId = run.Id.ToString(), BranchId = "br-1", RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        PayrollRunTestHelpers.SeedPayrollGlAccounts(db);
        db.GlAccounts.Add(LoanTestFixtures.GlAccount(code: "1220"));
        await db.SaveChangesAsync(Ct);

        PostGlJournalCommand? posted = null;
        var gl = Substitute.For<ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>>>();
        gl.HandleAsync(Arg.Do<PostGlJournalCommand>(c => posted = c), Arg.Any<CancellationToken>()).Returns(Result.Success<GlJournalResponse>(default!));
        var handler = new ApprovePayrollRunCommandHandler(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success<ApprovalRequestResponse>(default!)),
            gl,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success<NotificationResponse>(default!)),
            LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new ApprovePayrollRunCommand(run.Id, "approver1", null), Ct);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        var accounts = db.GlAccounts.ToDictionary(a => a.Code, a => a.Id);
        posted!.Lines.Single(l => l.AccountId == accounts["1220"]).CreditAmount.Should().Be(32m);
        posted.Lines.Single(l => l.AccountId == accounts["2350"]).CreditAmount.Should().Be(100m);
        posted.Lines.Sum(l => l.DebitAmount).Should().Be(posted.Lines.Sum(l => l.CreditAmount), "the journal still balances");
    }

    [Fact]
    public async Task HR_Cannot_Create_Edit_Or_Delete_A_Trip_Shortage_Deduction()
    {
        var create = new CreateEmployeeDeductionValidator().Validate(new CreateEmployeeDeductionCommand(Guid.NewGuid(), "trip-short", "Shortage", 10m,
            DateTimeOffset.UtcNow, null, "active"));
        create.IsValid.Should().BeFalse("the code is reserved for the trip close");

        var (db, employee) = await PayrollRunTestHelpers.SeedBranchAndEmployeeAsync();
        await using var _ = db;
        var deduction = HrTestFixtures.EmployeeDeduction(employee.Id, code: EmployeeDeduction.TripShortageCode, amount: 32m);
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(Ct);
        var permissions = LoanTestFixtures.AllowAllPermissionService();

        var update = await new UpdateEmployeeDeductionCommandHandler(db, permissions).HandleAsync(new UpdateEmployeeDeductionCommand(deduction.Id,
            EmployeeDeduction.TripShortageCode, "Shortage", 1m, deduction.EffectiveDate, null, "inactive"), Ct);
        var delete = await new DeleteEmployeeDeductionCommandHandler(db, permissions).HandleAsync(new DeleteEmployeeDeductionCommand(deduction.Id), Ct);

        update.Error!.Code.Should().Be("EmployeeDeduction.TripShortage");
        delete.Error!.Code.Should().Be("EmployeeDeduction.TripShortage");
        db.EmployeeDeductions.Single().Amount.Should().Be(32m);
    }
}
