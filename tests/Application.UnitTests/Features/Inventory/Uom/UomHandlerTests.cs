namespace ZARI.Application.UnitTests.Features.Inventory.Uom;

using ZARI.Application.Features.Inventory.Uoms.Create;
using ZARI.Application.Features.Inventory.Uoms.Delete;
using ZARI.Application.Features.Inventory.Uoms.Get;
using ZARI.Application.Features.Inventory.Uoms.GetAll;
using ZARI.Application.Features.Inventory.Uoms.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateUomCommandHandlerTests
{
    private static CreateUomCommand Command(string code = "PC") => new(code, "Piece");

    [Fact]
    public async Task HandleAsync_Should_Create_Uom()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("PC");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("UOMS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateUomCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.Uoms.Add(InventoryTestFixtures.Uom(code: "PC"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateUomCommandHandlerTests
{
    private static UpdateUomCommand Command(Guid id, string code = "PC") => new(id, code, "Piece Updated");

    [Fact]
    public async Task HandleAsync_Should_Update_Uom()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Uoms.FindAsync([uom.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Piece Updated");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("UOMS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateUomCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom(code: "PC");
        var other = InventoryTestFixtures.Uom(code: "BOX");
        db.Uoms.AddRange(uom, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(uom.Id, code: "BOX"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeleteUomCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Uom()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteUomCommand(uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Uoms.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteUomCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("UOMS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteUomCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteUomCommand(uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_In_Use_By_An_Item()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Items.Add(InventoryTestFixtures.Item(uom.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteUomCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteUomCommand(uom.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class GetUomQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Uom_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetUomQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetUomQuery(uom.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(uom.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetUomQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetUomQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("UOMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetUomQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetUomQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllUomsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Uoms()
    {
        await using var db = TestDbContextFactory.Create();
        db.Uoms.AddRange(InventoryTestFixtures.Uom(code: "PC"), InventoryTestFixtures.Uom(code: "BOX"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllUomsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllUomsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("UOMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllUomsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllUomsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
