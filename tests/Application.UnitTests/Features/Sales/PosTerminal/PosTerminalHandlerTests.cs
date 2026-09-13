namespace ZARI.Application.UnitTests.Features.Sales.PosTerminal;

using ZARI.Application.Features.Sales.PosTerminals.Create;
using ZARI.Application.Features.Sales.PosTerminals.Delete;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Application.Features.Sales.PosTerminals.GetAll;
using ZARI.Application.Features.Sales.PosTerminals.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreatePosTerminalCommandHandlerTests
{
    private static CreatePosTerminalCommand Command(string branchId, string code = "POS1") => new(branchId, code, "Test Terminal", null, null, null, null, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Terminal()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("POS1");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Create, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePosTerminalCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command("br-1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("nope"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists_At_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        db.PosTerminals.Add(SalesTestFixtures.PosTerminal(branch.Id, code: "POS1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdatePosTerminalCommandHandlerTests
{
    private static UpdatePosTerminalCommand Command(Guid id, string code = "POS1") => new(id, code, "Terminal Updated", null, null, null, null, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Terminal()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(terminal.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.PosTerminals.FindAsync([terminal.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Edit, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdatePosTerminalCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(terminal.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts_At_Same_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id, code: "POS1");
        var other = SalesTestFixtures.PosTerminal(branch.Id, code: "POS2");
        db.PosTerminals.AddRange(terminal, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(terminal.Id, code: "POS2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeletePosTerminalCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.Branch branch, ZARI.Domain.Entities.PosTerminal terminal)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch, terminal);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Terminal()
    {
        var (db, _, terminal) = await Seed();
        var handler = new DeletePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePosTerminalCommand(terminal.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.PosTerminals.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePosTerminalCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branch, terminal) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Delete, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeletePosTerminalCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeletePosTerminalCommand(terminal.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_In_Use_By_A_Sales_Invoice()
    {
        var (db, branch, terminal) = await Seed();
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = new ZARI.Domain.Entities.SalesInvoice
        {
            InvoiceNo = $"SINV-{Guid.NewGuid():N}", BranchId = branch.Id, CustomerId = customer.Id,
            InvoiceDate = DateTimeOffset.UtcNow, Status = "DRAFT", PosTerminalId = terminal.Id,
            Lines = [new ZARI.Domain.Entities.SalesInvoiceLine { ItemId = item.Id, Qty = 1, UomId = uom.Id, UnitPrice = 10 }]
        };
        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePosTerminalCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePosTerminalCommand(terminal.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await db.DisposeAsync();
    }
}

public sealed class GetPosTerminalQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Terminal_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPosTerminalQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPosTerminalQuery(terminal.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(terminal.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPosTerminalQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPosTerminalQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var terminal = SalesTestFixtures.PosTerminal(branch.Id);
        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.View, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPosTerminalQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPosTerminalQuery(terminal.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllPosTerminalsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Filter_By_Branch_When_Given()
    {
        await using var db = TestDbContextFactory.Create();
        var branch1 = LoanTestFixtures.Branch(id: "br-1");
        var branch2 = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.AddRange(branch1, branch2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.PosTerminals.AddRange(SalesTestFixtures.PosTerminal(branch1.Id, code: "POS1"), SalesTestFixtures.PosTerminal(branch2.Id, code: "POS2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPosTerminalsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPosTerminalsQuery(branch1.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Silently_Skip_Rows_Without_Branch_Permission()
    {
        await using var db = TestDbContextFactory.Create();
        var branch1 = LoanTestFixtures.Branch(id: "br-1");
        var branch2 = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.AddRange(branch1, branch2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.PosTerminals.AddRange(SalesTestFixtures.PosTerminal(branch1.Id, code: "POS1"), SalesTestFixtures.PosTerminal(branch2.Id, code: "POS2"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.View, "br-2", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllPosTerminalsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllPosTerminalsQuery(null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(t => t.Code == "POS1");
    }
}
