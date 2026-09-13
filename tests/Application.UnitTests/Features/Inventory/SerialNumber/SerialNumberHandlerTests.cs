namespace ZARI.Application.UnitTests.Features.Inventory.SerialNumber;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Unlike StockLedger/StockLocationBalance, none of these 5 handlers open their own transaction —
/// every one is a plain SaveChangesAsync, so all of them (including success paths) are fully
/// InMemory-testable.
/// </summary>
public sealed class ReceiveSerialCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_A_New_InStock_Serial()
    {
        await using var db = TestDbContextFactory.Create();
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
        var handler = new ReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveSerialCommand(item.Id, "SN-001", warehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("IN_STOCK");
        db.SerialNumbers.Should().ContainSingle(s => s.SerialNo == "SN-001");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Item_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveSerialCommand(Guid.NewGuid(), "SN-001", Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Item.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Warehouse_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveSerialCommand(item.Id, "SN-001", Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Warehouse.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_InStock_At_A_Different_Warehouse()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var warehouse1 = InventoryTestFixtures.Warehouse(branch.Id, code: "WH1");
        var warehouse2 = InventoryTestFixtures.Warehouse(branch.Id, code: "WH2");
        db.Warehouses.AddRange(warehouse1, warehouse2);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse1.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveSerialCommand(item.Id, "SN-001", warehouse2.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SerialNumber.AlreadyInStock");
    }

    [Fact]
    public async Task HandleAsync_Should_Re_Receive_A_Previously_Issued_Serial()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "SOLD" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReceiveSerialCommand(item.Id, "SN-001", warehouse.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("IN_STOCK");
        db.SerialNumbers.Should().ContainSingle();
    }
}

public sealed class IssueSerialCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Issue_An_InStock_Serial()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new IssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new IssueSerialCommand(item.Id, "SN-001", "SOLD"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.SerialNumbers.FirstAsync(s => s.SerialNo == "SN-001", TestContext.Current.CancellationToken)).Status.Should().Be("SOLD");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Serial_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new IssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new IssueSerialCommand(Guid.NewGuid(), "SN-MISSING", "SOLD"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SerialNumber.NotInStock");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Serial_Is_Not_Currently_InStock()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_TRANSIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new IssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new IssueSerialCommand(item.Id, "SN-001", "SOLD"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SerialNumber.NotInStock");
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_When_Already_At_Target_Disposition()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "SOLD" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new IssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new IssueSerialCommand(item.Id, "SN-001", "SOLD"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ReverseIssueSerialCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Restore_A_Sold_Serial_Back_To_InStock()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "SOLD" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReverseIssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseIssueSerialCommand(item.Id, "SN-001"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.SerialNumbers.FirstAsync(s => s.SerialNo == "SN-001", TestContext.Current.CancellationToken)).Status.Should().Be("IN_STOCK");
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_When_Serial_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReverseIssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseIssueSerialCommand(Guid.NewGuid(), "SN-MISSING"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_When_Already_InStock()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReverseIssueSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseIssueSerialCommand(item.Id, "SN-001"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ReverseReceiveSerialCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Remove_The_Serial_When_RevertTo_Is_Remove()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReverseReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseReceiveSerialCommand(item.Id, "SN-001", "REMOVE"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.SerialNumbers.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Revert_To_InTransit_When_RevertTo_Is_Not_Remove()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ReverseReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseReceiveSerialCommand(item.Id, "SN-001", "IN_TRANSIT"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.SerialNumbers.FirstAsync(s => s.SerialNo == "SN-001", TestContext.Current.CancellationToken)).Status.Should().Be("IN_TRANSIT");
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_When_Serial_Does_Not_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new ReverseReceiveSerialCommandHandler(db);

        var result = await handler.HandleAsync(new ReverseReceiveSerialCommand(Guid.NewGuid(), "SN-MISSING", "REMOVE"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class GetAllSerialNumbersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Serials_Ordered_By_SerialNo()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-002", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        db.SerialNumbers.Add(new SerialNumber { ItemId = item.Id, SerialNo = "SN-001", WarehouseId = warehouse.Id, Status = "IN_STOCK" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllSerialNumbersQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllSerialNumbersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].SerialNo.Should().Be("SN-001");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SERIAL_NUMBERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllSerialNumbersQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllSerialNumbersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
