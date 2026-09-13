namespace ZARI.Application.UnitTests.Features.Inventory.StockTransferRequest;

using ZARI.Application.Features.Inventory.StockTransferRequests.Submit;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class SubmitStockTransferRequestCommandHandlerTests
{
    private static SubmitStockTransferRequestCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>>(Result.Success(Dummy())),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static ApprovalRequestResponse Dummy() => new(Guid.NewGuid(), "STOCK_TRANSFER_REQUEST", "x", "br-2", "encoder", DateTimeOffset.UtcNow, "PENDING", "SUBMIT", null, [], DateTimeOffset.UtcNow);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockTransferRequest request, ZARI.Domain.Entities.Branch destBranch)> Seed(string status = "DRAFT", bool withLines = true)
    {
        var db = TestDbContextFactory.Create();
        var sourceBranch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(sourceBranch);
        var destBranch = LoanTestFixtures.Branch("br-2");
        db.Branches.Add(destBranch);
        var sourceWarehouse = InventoryTestFixtures.Warehouse(sourceBranch.Id);
        db.Warehouses.Add(sourceWarehouse);
        var destWarehouse = InventoryTestFixtures.Warehouse(destBranch.Id, code: "WH2");
        db.Warehouses.Add(destWarehouse);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = InventoryTestFixtures.StockTransferRequest(sourceBranch.Id, sourceWarehouse.Id, destBranch.Id, destWarehouse.Id, item.Id, uom.Id, status: status);
        if (!withLines)
            request.Lines.Clear();
        db.StockTransferRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request, destBranch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new SubmitStockTransferRequestCommand(Guid.NewGuid(), "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, request, destBranch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_TRANSFER_REQUESTS", FormAction.Edit, destBranch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new SubmitStockTransferRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, request, _) = await Seed(status: "APPROVED");

        var result = await Handler(db).HandleAsync(new SubmitStockTransferRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockTransferRequest.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, request, _) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new SubmitStockTransferRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockTransferRequest.NoLines");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_For_Approval()
    {
        var (db, request, _) = await Seed();

        var result = await Handler(db).HandleAsync(new SubmitStockTransferRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        await db.DisposeAsync();
    }
}
