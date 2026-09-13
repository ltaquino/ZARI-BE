namespace ZARI.Application.UnitTests.Features.SystemModule.Forms;

using ZARI.Application.Features.SystemModule.Forms.GetAll;
using ZARI.Application.UnitTests.TestSupport;

public sealed class GetAllFormsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Forms_Ordered_By_Module_Then_Name()
    {
        await using var db = TestDbContextFactory.Create();
        db.Forms.AddRange(
            SystemModuleTestFixtures.Form(code: "SALES_INVOICES", name: "Sales Invoices", module: "Sales"),
            SystemModuleTestFixtures.Form(code: "BRANCHES", name: "Branches", module: "System"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllFormsQueryHandler(db);

        var result = await handler.HandleAsync(new GetAllFormsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(f => f.Module).Should().ContainInOrder("Sales", "System");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Forms_Seeded()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllFormsQueryHandler(db);

        var result = await handler.HandleAsync(new GetAllFormsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
