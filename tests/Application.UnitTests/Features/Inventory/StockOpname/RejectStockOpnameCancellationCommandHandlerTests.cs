namespace ZARI.Application.UnitTests.Features.Inventory.StockOpname;

using ZARI.Application.Features.Inventory.StockOpnames.RejectCancellation;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RejectStockOpnameCancellationCommandHandlerTests
{
    private static RejectStockOpnameCancellationCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "STOCK_OPNAME", "x", "br-1", "admin.hq", DateTimeOffset.UtcNow, "REJECTED", "CANCEL", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, StockOpname opname, Branch branch)> Seed(string status = "PENDING_CANCELLATION", bool withApprovalRequest = true)
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
        opname.CancelReason = "wrong count";
        db.StockOpnames.Add(opname);
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest
            {
                EntityType = "STOCK_OPNAME", EntityId = opname.Id.ToString(), BranchId = branch.Id,
                RequestedBy = "manager", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "CANCEL"
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, opname, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new RejectStockOpnameCancellationCommand(Guid.NewGuid(), "admin.hq", "stands"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, opname, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasCancellationAuthorityAsync("STOCK_OPNAMES", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new RejectStockOpnameCancellationCommand(opname.Id, "admin.hq", "stands"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Pending_Cancellation()
    {
        var (db, opname, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new RejectStockOpnameCancellationCommand(opname.Id, "admin.hq", "stands"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockOpname.NotPendingCancellation");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Cancellation_Request_Found()
    {
        var (db, opname, _) = await Seed(withApprovalRequest: false);

        var result = await Handler(db).HandleAsync(new RejectStockOpnameCancellationCommand(opname.Id, "admin.hq", "stands"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Cancellation_Back_To_Posted()
    {
        var (db, opname, _) = await Seed();

        var result = await Handler(db).HandleAsync(new RejectStockOpnameCancellationCommand(opname.Id, "admin.hq", "stands"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("POSTED");
        await db.DisposeAsync();
    }
}
