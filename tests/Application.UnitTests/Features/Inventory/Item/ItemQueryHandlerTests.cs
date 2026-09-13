namespace ZARI.Application.UnitTests.Features.Inventory.Item;

using ZARI.Application.Features.Inventory.Items.GetAllPaged;
using ZARI.Application.Features.Inventory.Items.GetByIds;
using ZARI.Application.Features.Inventory.Items.Search;
using ZARI.Application.Features.Inventory.Items.TileMenu;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllItemsPagedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_A_Page_Of_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 3; i++) db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: $"ITEM-{i}"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllItemsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllItemsPagedQuery(Page: 1, PageSize: 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_Should_Filter_By_Search_On_Code_Or_Name()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: "FINDME"));
        db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: "OTHER"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllItemsPagedQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllItemsPagedQuery(Page: 1, PageSize: 20, Search: "FINDME"), TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllItemsPagedQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllItemsPagedQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetItemsByIdsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Only_The_Requested_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item1 = InventoryTestFixtures.Item(uom.Id, code: "ITEM-1");
        var item2 = InventoryTestFixtures.Item(uom.Id, code: "ITEM-2");
        db.Items.AddRange(item1, item2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetItemsByIdsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemsByIdsQuery([item1.Id]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(i => i.Id == item1.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_List_When_No_Ids_Given()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetItemsByIdsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetItemsByIdsQuery([]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetItemsByIdsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetItemsByIdsQuery([Guid.NewGuid()]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class SearchItemsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Rank_Code_Prefix_Matches_First()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var prefixMatch = InventoryTestFixtures.Item(uom.Id, code: "ABC-1", name: "Something");
        var substringMatch = InventoryTestFixtures.Item(uom.Id, code: "XYZ-1", name: "Contains ABC in name");
        db.Items.AddRange(substringMatch, prefixMatch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SearchItemsQuery("ABC"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Code.Should().Be("ABC-1");
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Inactive_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: "ABC-1", status: "inactive"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SearchItemsQuery("ABC"), TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_Query_Is_Blank()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new SearchItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SearchItemsQuery("  "), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Cap_Limit_To_Default_When_Out_Of_Range()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 10; i++) db.Items.Add(InventoryTestFixtures.Item(uom.Id, code: $"ABC-{i}"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new SearchItemsQuery("ABC", Limit: 999), TestContext.Current.CancellationToken);

        result.Value.Should().HaveCount(8);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SearchItemsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new SearchItemsQuery("ABC"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetTileMenuItemsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Sold_TileDisplay_Items_With_An_Active_Branch_Setting()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id, code: "TILE-1");
        item.IsTileDisplay = true;
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var setting = InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id);
        setting.SellingPrice = 55;
        db.ItemBranchSettings.Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTileMenuItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTileMenuItemsQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Single();
        row.Item.Code.Should().Be("TILE-1");
        row.Price.Should().Be(55);
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Items_Without_TileDisplay()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(item.Id, branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTileMenuItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTileMenuItemsQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Exclude_Items_Without_An_Active_Setting_At_This_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var otherBranch = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.AddRange(branch, otherBranch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        item.IsTileDisplay = true;
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ItemBranchSettings.Add(InventoryTestFixtures.ItemBranchSetting(item.Id, otherBranch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTileMenuItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetTileMenuItemsQuery(branch.Id), TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_MODE", FormAction.View, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        permissions.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetTileMenuItemsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetTileMenuItemsQuery("br-1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
