namespace ZARI.Application.UnitTests.Features.Inventory.ItemCategory;

using ZARI.Application.Features.Inventory.ItemCategories.Create;
using ZARI.Application.Features.Inventory.ItemCategories.Delete;
using ZARI.Application.Features.Inventory.ItemCategories.Get;
using ZARI.Application.Features.Inventory.ItemCategories.GetAll;
using ZARI.Application.Features.Inventory.ItemCategories.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateItemCategoryCommandHandlerTests
{
    private static CreateItemCategoryCommand Command(string code = "CAT1", Guid? parentCategoryId = null) => new(code, "Test Category", parentCategoryId);

    [Fact]
    public async Task HandleAsync_Should_Create_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("CAT1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_CATEGORIES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateItemCategoryCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.ItemCategories.Add(InventoryTestFixtures.ItemCategory(code: "CAT1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Parent_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(parentCategoryId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.ParentNotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Category_Under_A_Real_Parent()
    {
        await using var db = TestDbContextFactory.Create();
        var parent = InventoryTestFixtures.ItemCategory(code: "PARENT");
        db.ItemCategories.Add(parent);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(parentCategoryId: parent.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParentCategoryId.Should().Be(parent.Id);
    }
}

public sealed class UpdateItemCategoryCommandHandlerTests
{
    private static UpdateItemCategoryCommand Command(Guid id, string code = "CAT1", Guid? parentCategoryId = null) => new(id, code, "Updated", parentCategoryId);

    [Fact]
    public async Task HandleAsync_Should_Update_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.ItemCategories.FindAsync([category.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_CATEGORIES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateItemCategoryCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(category.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory(code: "CAT1");
        var other = InventoryTestFixtures.ItemCategory(code: "CAT2");
        db.ItemCategories.AddRange(category, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(category.Id, code: "CAT2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Parent_Is_Itself()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(category.Id, parentCategoryId: category.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.InvalidParent");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Parent_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(category.Id, parentCategoryId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.ParentNotFound");
    }
}

public sealed class DeleteItemCategoryCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Category()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.ItemCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_CATEGORIES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteItemCategoryCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(category.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Has_Child_Categories()
    {
        await using var db = TestDbContextFactory.Create();
        var parent = InventoryTestFixtures.ItemCategory(code: "PARENT");
        db.ItemCategories.Add(parent);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemCategories.Add(InventoryTestFixtures.ItemCategory(code: "CHILD", parentCategoryId: parent.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(parent.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.HasChildren");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_In_Use_By_An_Item()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        item.CategoryId = category.Id;
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteItemCategoryCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteItemCategoryCommand(category.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.InUse");
    }
}

public sealed class GetItemCategoryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Category_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var category = InventoryTestFixtures.ItemCategory();
        db.ItemCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetItemCategoryQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemCategoryQuery(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(category.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetItemCategoryQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemCategoryQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_CATEGORIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetItemCategoryQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetItemCategoryQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllItemCategoriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Categories()
    {
        await using var db = TestDbContextFactory.Create();
        db.ItemCategories.AddRange(InventoryTestFixtures.ItemCategory(code: "CAT1"), InventoryTestFixtures.ItemCategory(code: "CAT2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllItemCategoriesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllItemCategoriesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEM_CATEGORIES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllItemCategoriesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllItemCategoriesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
