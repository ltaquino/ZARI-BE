namespace ZARI.Application.UnitTests.Features.Inventory.StockOpname;

using ZARI.Application.Features.Inventory.StockOpnames.RequestCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RequestStockOpnameCancellationCommandHandlerTests
{
    private static RequestStockOpnameCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "STOCK_OPNAME", "x", "br-1", "manager", DateTimeOffset.UtcNow, "PENDING", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, StockOpname opname, Branch branch)> Seed(string status = "POSTED")
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
        var opname = InventoryTestFixtures.StockOpname(branch.Id, warehouse.Id, item.Id, status: status);
        db.StockOpnames.Add(opname);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, opname, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RequestStockOpnameCancellationCommand(Guid.NewGuid(), "manager", "wrong count"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, opname, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_OPNAMES", FormAction.Cancel, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RequestStockOpnameCancellationCommand(opname.Id, "manager", "wrong count"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Posted()
    {
        var (db, opname, _) = await Seed(status: "DRAFT");

        var result = await Handler(db).HandleAsync(new RequestStockOpnameCancellationCommand(opname.Id, "manager", "wrong count"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockOpname.NotPosted");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Request_Cancellation()
    {
        var (db, opname, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RequestStockOpnameCancellationCommand(opname.Id, "manager", "wrong count"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_CANCELLATION");
        await db.DisposeAsync();
    }
}
