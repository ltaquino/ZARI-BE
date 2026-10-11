namespace ZARI.Application.UnitTests.Features.Inventory.ItemBarcode;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Inventory.ItemBarcodes;
using ZARI.Application.Features.Inventory.Items.Search;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class ItemBarcodeHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid rice, Guid sugar)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var rice = InventoryTestFixtures.Item(uom.Id, code: "RICE-25");
        var sugar = InventoryTestFixtures.Item(uom.Id, code: "SUGAR-1");
        db.Items.AddRange(rice, sugar);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, rice.Id, sugar.Id);
    }

    private static CreateItemBarcodeCommandHandler Create(ZARI.Infrastructure.Persistence.AppDbContext db) => new(db, LoanTestFixtures.AllowAllPermissionService());

    [Fact]
    public async Task An_Item_Can_Have_A_Piece_And_A_Box_Barcode_And_A_Scan_Resolves_To_Its_Pack()
    {
        var (db, rice, _) = await Seed();
        var ct = TestContext.Current.CancellationToken;
        (await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, " 4800000000015 "), ct)).IsSuccess.Should().BeTrue();
        var box = await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, "14800000000012", 12, "Box of 12"), ct);

        box.Value!.ItemCode.Should().Be("RICE-25");
        var lookup = await new LookupItemBarcodeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new LookupItemBarcodeQuery("14800000000012"), ct);
        lookup.Value!.Item.Code.Should().Be("RICE-25");
        lookup.Value.PackQty.Should().Be(12);
        var list = await new GetItemBarcodesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(new GetItemBarcodesQuery(rice), ct);
        list.Value!.Select(b => b.Barcode).Should().Equal("4800000000015", "14800000000012");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task A_Barcode_Already_On_Another_Item_Is_Refused()
    {
        var (db, rice, sugar) = await Seed();
        var ct = TestContext.Current.CancellationToken;
        await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, "4800000000015"), ct);

        var result = await Create(db).HandleAsync(new CreateItemBarcodeCommand(sugar, "4800000000015"), ct);

        result.Error!.Code.Should().Be("ItemBarcode.Duplicate");
        result.Error.Message.Should().Contain("RICE-25");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task An_Unknown_Or_Inactive_Items_Barcode_Does_Not_Resolve()
    {
        var (db, rice, _) = await Seed();
        var ct = TestContext.Current.CancellationToken;
        await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, "4800000000015"), ct);
        var handler = new LookupItemBarcodeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        (await handler.HandleAsync(new LookupItemBarcodeQuery("999"), ct)).Error!.Type.Should().Be(ErrorType.NotFound);
        (await db.Items.SingleAsync(i => i.Id == rice, ct)).Status = "inactive";
        await db.SaveChangesAsync(ct);
        (await handler.HandleAsync(new LookupItemBarcodeQuery("4800000000015"), ct)).Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Removing_A_Barcode_Frees_It()
    {
        var (db, rice, sugar) = await Seed();
        var ct = TestContext.Current.CancellationToken;
        var added = await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, "4800000000015"), ct);

        (await new DeleteItemBarcodeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(new DeleteItemBarcodeCommand(added.Value!.Id), ct))
            .IsSuccess.Should().BeTrue();

        (await Create(db).HandleAsync(new CreateItemBarcodeCommand(sugar, "4800000000015"), ct)).IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Changing_Barcodes_Needs_Item_Edit_Permission()
    {
        var (db, rice, _) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("ITEMS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new CreateItemBarcodeCommandHandler(db, permissions).HandleAsync(new CreateItemBarcodeCommand(rice, "4800000000015"), TestContext.Current.CancellationToken);

        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Theory]
    [InlineData("48 000", false)]
    [InlineData("", false)]
    [InlineData("4800000000015", true)]
    [InlineData("ABC-123/4", true)]
    public void Barcode_Characters_Are_What_A_Scanner_Types(string barcode, bool valid) =>
        new CreateItemBarcodeValidator().Validate(new CreateItemBarcodeCommand(Guid.NewGuid(), barcode)).IsValid.Should().Be(valid);

    [Fact]
    public async Task Item_Search_Puts_An_Exact_Barcode_Match_First()
    {
        var (db, rice, _) = await Seed();
        var ct = TestContext.Current.CancellationToken;
        await Create(db).HandleAsync(new CreateItemBarcodeCommand(rice, "SUGAR-1X"), ct); // a barcode that also looks like a code

        var result = await new SearchItemsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(new SearchItemsQuery("SUGAR-1X", 8), ct);

        result.Value!.Select(i => i.Code).Should().Equal("RICE-25");
        await db.DisposeAsync();
    }
}
