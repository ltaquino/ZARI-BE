namespace ZARI.Application.UnitTests.Features.Purchasing.PurchaseRequest;

using ZARI.Application.Features.Purchasing.PurchaseRequests.Submit;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class SubmitPurchaseRequestCommandHandlerTests
{
    private static SubmitPurchaseRequestCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, new SubmitForApprovalCommandHandler(db), new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, PurchaseRequest request)> Seed(string status = "DRAFT", bool withLines = true)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = PurchasingTestFixtures.PurchaseRequest(branch.Id, item.Id, uom.Id, status: status);
        if (!withLines) request.Lines.Clear();
        db.PurchaseRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, request);
    }

    [Fact]
    public async Task HandleAsync_Should_Submit_Draft_For_Approval()
    {
        var (db, request) = await Seed();

        var result = await Handler(db).HandleAsync(new SubmitPurchaseRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_APPROVAL");
        db.ApprovalRequests.Should().ContainSingle(r => r.EntityType == "PURCHASE_REQUEST" && r.EntityId == request.Id.ToString());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new SubmitPurchaseRequestCommand(Guid.NewGuid(), "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, request) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("PURCHASE_REQUESTS", FormAction.Edit, request.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new SubmitPurchaseRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, request) = await Seed(status: "APPROVED");

        var result = await Handler(db).HandleAsync(new SubmitPurchaseRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseRequest.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, request) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new SubmitPurchaseRequestCommand(request.Id, "encoder"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PurchaseRequest.NoLines");
        await db.DisposeAsync();
    }
}
