namespace ZARI.Application.UnitTests.Features.Inventory.StockOpname;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Inventory.StockOpnames.Post;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.StockLedgers.Issue;
using ZARI.Application.Features.Inventory.StockLedgers.Receive;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// No approval workflow here (StockOpname posts directly, DRAFT -> POSTED) — but the real success
/// path still isn't InMemory-testable: ReceiveStockCommand/IssueStockLinesCommand each open a real
/// transaction (faked here), and the handler's own final status flip uses ExecuteUpdateAsync (also
/// unsupported). Only the guard clauses before that point are exercised.
/// </summary>
public sealed class PostStockOpnameCommandHandlerTests
{
    private static PostStockOpnameCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db,
            LoanTestFixtures.SuccessHandler<ReceiveStockCommand, Result<ReceiveStockResponse>>(Result.Success(new ReceiveStockResponse(50))),
            LoanTestFixtures.SuccessHandler<IssueStockLinesCommand, Result<IssueStockLinesResponse>>(Result.Success(new IssueStockLinesResponse(new Dictionary<string, decimal>()))),
            LoanTestFixtures.SuccessHandler<ReceiveSerialCommand, Result<SerialNumberResponse>>(Result.Failure<SerialNumberResponse>(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<IssueSerialCommand, Result>(Result.Failure(Error.Failure("x", "unused"))),
            LoanTestFixtures.SuccessHandler<PostGlJournalCommand, Result<GlJournalResponse>>(Result.Failure<GlJournalResponse>(Error.Failure("x", "unused"))),
            new CreateNotificationCommandHandler(db), permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.StockOpname opname, ZARI.Domain.Entities.Branch branch)> Seed(string status = "DRAFT", bool withLines = true)
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
        if (!withLines)
            opname.Lines.Clear();
        db.StockOpnames.Add(opname);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, opname, branch);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new PostStockOpnameCommand(Guid.NewGuid(), "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, opname, branch) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("STOCK_OPNAMES", FormAction.Approve, branch.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new PostStockOpnameCommand(opname.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, opname, _) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new PostStockOpnameCommand(opname.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockOpname.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Lines()
    {
        var (db, opname, _) = await Seed(withLines: false);

        var result = await Handler(db).HandleAsync(new PostStockOpnameCommand(opname.Id, "manager"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("StockOpname.NoLines");
        await db.DisposeAsync();
    }
}
