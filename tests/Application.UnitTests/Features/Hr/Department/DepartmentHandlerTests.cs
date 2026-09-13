namespace ZARI.Application.UnitTests.Features.Hr.Department;

using NSubstitute;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.Departments.Create;
using ZARI.Application.Features.Hr.Departments.Delete;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Application.Features.Hr.Departments.GetAll;
using ZARI.Application.Features.Hr.Departments.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateDepartmentCommandHandlerTests
{
    private static CreateDepartmentCommand Command(string code = "ACCT") => new(code, "Accounting", "active", null);

    [Fact]
    public async Task HandleAsync_Should_Create_Department()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("ACCT");
        result.Value.HeadEmployeeId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DEPARTMENTS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateDepartmentCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.Departments.Add(HrTestFixtures.Department(code: "ACCT"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_HeadEmployee_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var command = Command() with { HeadEmployeeId = Guid.NewGuid() };

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class UpdateDepartmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Department()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department(code: "ACCT");
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateDepartmentCommand(department.Id, "ACCT2", "Accounting Dept", "inactive", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Departments.FindAsync([department.Id], TestContext.Current.CancellationToken);
        updated!.Code.Should().Be("ACCT2");
        updated.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateDepartmentCommand(Guid.NewGuid(), "ACCT", "Accounting", "active", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DEPARTMENTS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateDepartmentCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new UpdateDepartmentCommand(department.Id, "DEPT-2", "Test", "active", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Code()
    {
        await using var db = TestDbContextFactory.Create();
        db.Departments.Add(HrTestFixtures.Department(code: "ACCT"));
        var department = HrTestFixtures.Department(code: "HR");
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateDepartmentCommand(department.Id, "ACCT", "HR", "active", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeleteDepartmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Department()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDepartmentCommand(department.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Departments.FindAsync([department.Id], TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDepartmentCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Positions()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        db.Positions.Add(HrTestFixtures.Position(departmentId: department.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteDepartmentCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDepartmentCommand(department.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetDepartmentQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Department()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetDepartmentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDepartmentQuery(department.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(department.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetDepartmentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDepartmentQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DEPARTMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetDepartmentQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetDepartmentQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllDepartmentsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Departments()
    {
        await using var db = TestDbContextFactory.Create();
        db.Departments.Add(HrTestFixtures.Department(code: "ACCT"));
        db.Departments.Add(HrTestFixtures.Department(code: "HR"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllDepartmentsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllDepartmentsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DEPARTMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllDepartmentsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllDepartmentsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
