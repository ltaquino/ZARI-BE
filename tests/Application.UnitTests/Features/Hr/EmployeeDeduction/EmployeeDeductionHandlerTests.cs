namespace ZARI.Application.UnitTests.Features.Hr.EmployeeDeduction;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.EmployeeDeductions.Create;
using ZARI.Application.Features.Hr.EmployeeDeductions.Delete;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Application.Features.Hr.EmployeeDeductions.GetAll;
using ZARI.Application.Features.Hr.EmployeeDeductions.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class EmployeeDeductionTestHelpers
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

public sealed class CreateEmployeeDeductionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        var handler = new CreateEmployeeDeductionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateEmployeeDeductionCommand(employeeId, "CASH-ADV", "Cash Advance", 500m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEE_DEDUCTIONS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateEmployeeDeductionCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new CreateEmployeeDeductionCommand(employeeId, "CASH-ADV", "Cash Advance", 500m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class UpdateEmployeeDeductionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        var deduction = HrTestFixtures.EmployeeDeduction(employeeId: employeeId);
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeDeductionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateEmployeeDeductionCommand(deduction.Id, "CASH-ADV", "Cash Advance v2", 750m, deduction.EffectiveDate, null, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.EmployeeDeductions.FindAsync([deduction.Id], TestContext.Current.CancellationToken);
        updated!.Amount.Should().Be(750m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateEmployeeDeductionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateEmployeeDeductionCommand(Guid.NewGuid(), "CASH-ADV", "Cash Advance", 500m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteEmployeeDeductionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        var deduction = HrTestFixtures.EmployeeDeduction(employeeId: employeeId);
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeDeductionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeDeductionCommand(deduction.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteEmployeeDeductionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeDeductionCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetEmployeeDeductionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Deduction()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        var deduction = HrTestFixtures.EmployeeDeduction(employeeId: employeeId);
        db.EmployeeDeductions.Add(deduction);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetEmployeeDeductionQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeDeductionQuery(deduction.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(deduction.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetEmployeeDeductionQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeDeductionQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllEmployeeDeductionsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Deductions()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeDeductionTestHelpers.SeedMasterData(db);
        db.EmployeeDeductions.Add(HrTestFixtures.EmployeeDeduction(employeeId: employeeId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllEmployeeDeductionsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllEmployeeDeductionsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
