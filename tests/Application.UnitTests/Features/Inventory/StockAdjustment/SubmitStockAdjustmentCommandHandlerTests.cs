namespace ZARI.Application.UnitTests.Features.Inventory.StockAdjustment;

using ZARI.Application.Features.Inventory.StockAdjustments.Submit;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class SubmitStockAdjustmentCommandHandlerTests
{
    private static SubmitStockAdjustmentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "STOCK_ADJUSTMENT", "x", "br-1", "encoder", DateTimeOffset.UtcNow, "PENDING", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockAdjustment adjustment, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT", bool withLines = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse = InventoryTestFixtures.Warehouse(branch.Id);
        db.Warehouses.Add(warehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var adjustment = InventoryTestFixtures.StockAdjustment(branch.Id, warehouse.Id, item.Id, status: status);
        if (!withLines)
            adjustment.Lines.Clear();
        db.StockAdjustments.Add(adjustment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, adjustment, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new SubmitStockAdjustmentCommand(Guid.NewGuid(), "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, adjustment, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_ADJUSTMENTS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new SubmitStockAdjustmentCommand(adjustment.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, adjustment, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new SubmitStockAdjustmentCommand(adjustment.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockAdjustment.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, adjustment, _) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new SubmitStockAdjustmentCommand(adjustment.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockAdjustment.NoLines");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_For_Approval()
    {
        var (db, adjustment, _) = await Seed();

        var result = await Handler(db).HandleAsync(new SubmitStockAdjustmentCommand(adjustment.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        await db.DisposeAsync();
    }
}
