namespace ZARI.Application.UnitTests.Features.Hr.Employee;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.Employees.Create;
using ZARI.Application.Features.Hr.Employees.Delete;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Application.Features.Hr.Employees.GetAll;
using ZARI.Application.Features.Hr.Employees.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Infrastructure.Persistence;

public static class EmployeeTestHelpers
{
    public static async Task<(string BranchId, Guid PositionId, Guid DepartmentId)> SeedMasterData(AppDbContext db)
    {
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (branch.Id, position.Id, department.Id);
    }
}

public sealed class CreateEmployeeCommandHandlerTests
{
    private static CreateEmployeeCommand Command(string branchId, Guid positionId, Guid departmentId, string employeeNo = "EMP-0001") =>
        new(employeeNo, "Juan", null, "Dela Cruz", null, null, null, null, null,
            branchId, positionId, departmentId, DateTimeOffset.UtcNow, "active", "PROBATIONARY",
            null, null, null, null, 20000m, null, null, null, null, null);

    [Fact]
    public async Task HandleAsync_Should_Create_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeNo.Should().Be("EMP-0001");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateEmployeeCommandHandler(db, permissions, IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_EmployeeNo()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0001", branchId: branchId, positionId: positionId, departmentId: departmentId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command(branchId, positionId, departmentId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var result = await handler.HandleAsync(Command("br-missing", position.Id, department.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class UpdateEmployeeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var command = new UpdateEmployeeCommand(employee.Id, employee.EmployeeNo, "Juan", null, "Santos", null, null, null, null, null,
            branchId, positionId, departmentId, employee.DateHired, "active", "REGULAR", DateTimeOffset.UtcNow, null, null,
            null, null, null, null, 25000m, null, null, null, null, null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Employees.FindAsync([employee.Id], TestContext.Current.CancellationToken);
        updated!.LastName.Should().Be("Santos");
        updated.EmploymentStatus.Should().Be("REGULAR");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService(), IdentityTestFixtures.UserManager(db));

        var command = new UpdateEmployeeCommand(Guid.NewGuid(), "EMP-0001", "Juan", null, "Santos", null, null, null, null, null,
            "br-1", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "active", "REGULAR", null, null, null,
            null, null, null, null, 25000m, null, null, null, null, null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteEmployeeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Is_Department_Head()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var department = await db.Departments.FindAsync([departmentId], TestContext.Current.CancellationToken);
        department!.HeadEmployeeId = employee.Id;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteEmployeeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteEmployeeCommand(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetEmployeeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Employee()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetEmployeeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeQuery(employee.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeNo.Should().Be(employee.EmployeeNo);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetEmployeeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetEmployeeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        var employee = HrTestFixtures.Employee(branchId: branchId, positionId: positionId, departmentId: departmentId);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("EMPLOYEES", FormAction.View, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetEmployeeQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetEmployeeQuery(employee.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllEmployeesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Employees()
    {
        await using var db = TestDbContextFactory.Create();
        var (branchId, positionId, departmentId) = await EmployeeTestHelpers.SeedMasterData(db);
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0001", branchId: branchId, positionId: positionId, departmentId: departmentId));
        db.Employees.Add(HrTestFixtures.Employee(employeeNo: "EMP-0002", branchId: branchId, positionId: positionId, departmentId: departmentId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllEmployeesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllEmployeesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("EMPLOYEES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllEmployeesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllEmployeesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
