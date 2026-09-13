namespace ZARI.Application.UnitTests.Features.Inventory.AdjustmentReason;

using ZARI.Application.Features.Inventory.AdjustmentReasons.Create;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Delete;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Application.Features.Inventory.AdjustmentReasons.GetAll;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateAdjustmentReasonCommandHandlerTests
{
    private static CreateAdjustmentReasonCommand Command(string code = "DAMAGE") => new(code, "Damaged in warehouse", null, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("DAMAGE");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateAdjustmentReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.AdjustmentReasons.Add(InventoryTestFixtures.AdjustmentReason(code: "DAMAGE"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateAdjustmentReasonCommandHandlerTests
{
    private static UpdateAdjustmentReasonCommand Command(Guid id, string code = "DAMAGE") => new(id, code, "Updated description", null, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason();
        db.AdjustmentReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.AdjustmentReasons.FindAsync([reason.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason();
        db.AdjustmentReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateAdjustmentReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(reason.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason(code: "DAMAGE");
        var other = InventoryTestFixtures.AdjustmentReason(code: "EXPIRED");
        db.AdjustmentReasons.AddRange(reason, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(reason.Id, code: "EXPIRED"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeleteAdjustmentReasonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason();
        db.AdjustmentReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteAdjustmentReasonCommand(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.AdjustmentReasons.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteAdjustmentReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteAdjustmentReasonCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason();
        db.AdjustmentReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteAdjustmentReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteAdjustmentReasonCommand(reason.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAdjustmentReasonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Reason_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = InventoryTestFixtures.AdjustmentReason();
        db.AdjustmentReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAdjustmentReasonQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAdjustmentReasonQuery(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(reason.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAdjustmentReasonQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAdjustmentReasonQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAdjustmentReasonQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAdjustmentReasonQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllAdjustmentReasonsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Reasons()
    {
        await using var db = TestDbContextFactory.Create();
        db.AdjustmentReasons.AddRange(InventoryTestFixtures.AdjustmentReason(code: "DAMAGE"), InventoryTestFixtures.AdjustmentReason(code: "EXPIRED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllAdjustmentReasonsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllAdjustmentReasonsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ADJUSTMENT_REASONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllAdjustmentReasonsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllAdjustmentReasonsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
