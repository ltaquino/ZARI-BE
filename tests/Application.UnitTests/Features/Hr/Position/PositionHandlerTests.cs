namespace ZARI.Application.UnitTests.Features.Hr.Position;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.Positions.Create;
using ZARI.Application.Features.Hr.Positions.Delete;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Application.Features.Hr.Positions.GetAll;
using ZARI.Application.Features.Hr.Positions.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreatePositionCommandHandlerTests
{
    private static async Task<Guid> SeedDepartment(ZARI.Infrastructure.Persistence.AppDbContext db)
    {
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return department.Id;
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Position()
    {
        await using var db = TestDbContextFactory.Create();
        var departmentId = await SeedDepartment(db);
        var handler = new CreatePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreatePositionCommand("ACCTG-CLERK", "Accounting Clerk", departmentId, "active", null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("ACCTG-CLERK");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var departmentId = await SeedDepartment(db);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POSITIONS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePositionCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new CreatePositionCommand("CLERK", "Clerk", departmentId, "active", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Department_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreatePositionCommand("CLERK", "Clerk", Guid.NewGuid(), "active", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate_Code()
    {
        await using var db = TestDbContextFactory.Create();
        var departmentId = await SeedDepartment(db);
        db.Positions.Add(HrTestFixtures.Position(code: "CLERK", departmentId: departmentId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new CreatePositionCommand("CLERK", "Clerk", departmentId, "active", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdatePositionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Position()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdatePositionCommand(position.Id, "POS-2", "Senior Clerk", department.Id, "active", 15000, 20000), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Positions.FindAsync([position.Id], TestContext.Current.CancellationToken);
        updated!.Name.Should().Be("Senior Clerk");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdatePositionCommand(Guid.NewGuid(), "POS-1", "Clerk", Guid.NewGuid(), "active", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeletePositionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Position()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePositionCommand(position.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Employees()
    {
        await using var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        db.Employees.Add(HrTestFixtures.Employee(positionId: position.Id, departmentId: department.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePositionCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePositionCommand(position.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetPositionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Position()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        var position = HrTestFixtures.Position(departmentId: department.Id);
        db.Positions.Add(position);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPositionQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPositionQuery(position.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(position.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPositionQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPositionQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllPositionsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Positions()
    {
        await using var db = TestDbContextFactory.Create();
        var department = HrTestFixtures.Department();
        db.Departments.Add(department);
        db.Positions.Add(HrTestFixtures.Position(code: "P1", departmentId: department.Id));
        db.Positions.Add(HrTestFixtures.Position(code: "P2", departmentId: department.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPositionsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPositionsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
