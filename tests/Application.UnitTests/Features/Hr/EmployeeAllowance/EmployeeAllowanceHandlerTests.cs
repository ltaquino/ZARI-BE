namespace ZARI.Application.UnitTests.Features.Hr.EmployeeAllowance;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.EmployeeAllowances.Create;
using ZARI.Application.Features.Hr.EmployeeAllowances.Delete;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Application.Features.Hr.EmployeeAllowances.GetAll;
using ZARI.Application.Features.Hr.EmployeeAllowances.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class EmployeeAllowanceTestHelpers
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

public sealed class CreateEmployeeAllowanceCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Allowance()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        var handler = new CreateEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateEmployeeAllowanceCommand(employeeId, "TRANSPO", "Transportation Allowance", 1000m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEE_ALLOWANCES", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateEmployeeAllowanceCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new CreateEmployeeAllowanceCommand(employeeId, "TRANSPO", "Transportation Allowance", 1000m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Employee_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreateEmployeeAllowanceCommand(Guid.NewGuid(), "TRANSPO", "Transportation Allowance", 1000m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class UpdateEmployeeAllowanceCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Allowance()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        var allowance = HrTestFixtures.EmployeeAllowance(employeeId: employeeId);
        db.EmployeeAllowances.Add(allowance);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateEmployeeAllowanceCommand(allowance.Id, "TRANSPO", "Transpo Allowance v2", 1500m, allowance.EffectiveDate, null, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.EmployeeAllowances.FindAsync([allowance.Id], TestContext.Current.CancellationToken);
        updated!.Amount.Should().Be(1500m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateEmployeeAllowanceCommand(Guid.NewGuid(), "TRANSPO", "Transportation Allowance", 1000m, DateTimeOffset.UtcNow, null, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteEmployeeAllowanceCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Allowance()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        var allowance = HrTestFixtures.EmployeeAllowance(employeeId: employeeId);
        db.EmployeeAllowances.Add(allowance);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeAllowanceCommand(allowance.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteEmployeeAllowanceCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeAllowanceCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetEmployeeAllowanceQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Allowance()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        var allowance = HrTestFixtures.EmployeeAllowance(employeeId: employeeId);
        db.EmployeeAllowances.Add(allowance);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetEmployeeAllowanceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeAllowanceQuery(allowance.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(allowance.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetEmployeeAllowanceQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeAllowanceQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllEmployeeAllowancesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Allowances()
    {
        await using var db = TestDbContextFactory.Create();
        var employeeId = await EmployeeAllowanceTestHelpers.SeedMasterData(db);
        db.EmployeeAllowances.Add(HrTestFixtures.EmployeeAllowance(employeeId: employeeId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllEmployeeAllowancesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllEmployeeAllowancesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
