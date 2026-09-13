namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseReturnReason;

using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Create;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Delete;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Get;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.GetAll;
using ZARI.Application.Features.Purchasing.PurchaseReturnReasons.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreatePurchaseReturnReasonCommandHandlerTests
{
    private static CreatePurchaseReturnReasonCommand Command(string code = "DAMAGED") => new(code, "Damaged goods", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("DAMAGED");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePurchaseReturnReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.PurchaseReturnReasons.Add(PurchasingTestFixtures.PurchaseReturnReason(code: "DAMAGED"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdatePurchaseReturnReasonCommandHandlerTests
{
    private static UpdatePurchaseReturnReasonCommand Command(Guid id, string code = "DAMAGED") => new(id, code, "Updated description", "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason();
        db.PurchaseReturnReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.PurchaseReturnReasons.FindAsync([reason.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason();
        db.PurchaseReturnReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdatePurchaseReturnReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(reason.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason(code: "DAMAGED");
        var other = PurchasingTestFixtures.PurchaseReturnReason(code: "WRONG_ITEM");
        db.PurchaseReturnReasons.AddRange(reason, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(reason.Id, code: "WRONG_ITEM"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeletePurchaseReturnReasonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Reason()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason();
        db.PurchaseReturnReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePurchaseReturnReasonCommand(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.PurchaseReturnReasons.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePurchaseReturnReasonCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePurchaseReturnReasonCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason();
        db.PurchaseReturnReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeletePurchaseReturnReasonCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeletePurchaseReturnReasonCommand(reason.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetPurchaseReturnReasonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Reason_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var reason = PurchasingTestFixtures.PurchaseReturnReason();
        db.PurchaseReturnReasons.Add(reason);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPurchaseReturnReasonQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseReturnReasonQuery(reason.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(reason.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPurchaseReturnReasonQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPurchaseReturnReasonQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPurchaseReturnReasonQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPurchaseReturnReasonQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllPurchaseReturnReasonsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Reasons()
    {
        await using var db = TestDbContextFactory.Create();
        db.PurchaseReturnReasons.AddRange(PurchasingTestFixtures.PurchaseReturnReason(code: "DAMAGED"), PurchasingTestFixtures.PurchaseReturnReason(code: "WRONG_ITEM"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPurchaseReturnReasonsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPurchaseReturnReasonsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PURCHASE_RETURN_REASONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllPurchaseReturnReasonsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllPurchaseReturnReasonsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
