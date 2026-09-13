namespace ZARI.Application.UnitTests.Features.Sales.DiscountRule;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Sales.DiscountRules.Create;
using ZARI.Application.Features.Sales.DiscountRules.Delete;
using ZARI.Application.Features.Sales.DiscountRules.Get;
using ZARI.Application.Features.Sales.DiscountRules.GetAll;
using ZARI.Application.Features.Sales.DiscountRules.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateDiscountRuleCommandHandlerTests
{
    private static CreateDiscountRuleCommand Command(string code = "DISC1", string scope = "ALL", List<Guid>? itemIds = null, Guid? categoryId = null, string? branchId = null, string discountType = "PERCENT", decimal discountValue = 10) =>
        new(code, "Test Discount", scope, itemIds ?? [], categoryId, discountType, discountValue, null, null, null, branchId, 1, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("DISC1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DISCOUNT_RULES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateDiscountRuleCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.DiscountRules.Add(SalesTestFixtures.DiscountRule(code: "DISC1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(scope: "ITEM", itemIds: [Guid.NewGuid()]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_With_Valid_Items()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(scope: "ITEM", itemIds: [item.Id]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemIds.Should().ContainSingle().Which.Should().Be(item.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Category_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(scope: "CATEGORY", categoryId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ItemCategory.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branchId: "nope"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}

public sealed class UpdateDiscountRuleCommandHandlerTests
{
    private static UpdateDiscountRuleCommand Command(Guid id, string code = "DISC1", string scope = "ALL", List<Guid>? itemIds = null, Guid? categoryId = null, string? branchId = null) =>
        new(id, code, "Updated", scope, itemIds ?? [], categoryId, "PERCENT", 15, null, null, null, branchId, 1, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule();
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(rule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.DiscountRules.FindAsync([rule.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule();
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DISCOUNT_RULES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateDiscountRuleCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(rule.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule(code: "DISC1");
        var other = SalesTestFixtures.DiscountRule(code: "DISC2");
        db.DiscountRules.AddRange(rule, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(rule.Id, code: "DISC2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Replace_Items_On_Update()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item1 = InventoryTestFixtures.Item(uom.Id, code: "ITEM-1");
        var item2 = InventoryTestFixtures.Item(uom.Id, code: "ITEM-2");
        db.Items.AddRange(item1, item2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var rule = SalesTestFixtures.DiscountRule(scope: "ITEM");
        rule.Items.Add(new ZARI.Domain.Entities.DiscountRuleItem { ItemId = item1.Id });
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(rule.Id, scope: "ITEM", itemIds: [item2.Id]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.DiscountRules.Include(r => r.Items).FirstAsync(r => r.Id == rule.Id, TestContext.Current.CancellationToken);
        updated.Items.Should().ContainSingle(i => i.ItemId == item2.Id);
    }
}

public sealed class DeleteDiscountRuleCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule();
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDiscountRuleCommand(rule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.DiscountRules.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteDiscountRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteDiscountRuleCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule();
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DISCOUNT_RULES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteDiscountRuleCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteDiscountRuleCommand(rule.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetDiscountRuleQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Rule_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = SalesTestFixtures.DiscountRule();
        db.DiscountRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetDiscountRuleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDiscountRuleQuery(rule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(rule.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetDiscountRuleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetDiscountRuleQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DISCOUNT_RULES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetDiscountRuleQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetDiscountRuleQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllDiscountRulesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Rules()
    {
        await using var db = TestDbContextFactory.Create();
        db.DiscountRules.AddRange(SalesTestFixtures.DiscountRule(code: "DISC1"), SalesTestFixtures.DiscountRule(code: "DISC2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllDiscountRulesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllDiscountRulesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("DISCOUNT_RULES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllDiscountRulesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllDiscountRulesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
