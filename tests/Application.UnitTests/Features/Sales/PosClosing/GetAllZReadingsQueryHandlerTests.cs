namespace ZARI.Application.UnitTests.Features.Sales.PosClosing;

using ZARI.Application.Features.Sales.PosClosing.GetAllZReadings;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllZReadingsQueryHandlerTests
{
    private static ZReading ZReading(string branchId, int counter) => new()
    {
        BranchId = branchId, ZCounterValue = counter, PeriodStart = DateTimeOffset.UtcNow.AddDays(-1), PeriodEnd = DateTimeOffset.UtcNow, InvoiceCount = 0
    };

    [Fact]
    public async Task HandleAsync_Should_Return_Readings_Ordered_By_ZCounter_Descending()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ZReadings.AddRange(ZReading(branch.Id, 1), ZReading(branch.Id, 2));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllZReadingsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllZReadingsQuery(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].ZCounterValue.Should().Be(2);
        result.Value![1].ZCounterValue.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Only_Return_Readings_For_The_Given_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch1 = LoanTestFixtures.Branch(id: "br-1");
        var branch2 = LoanTestFixtures.Branch(id: "br-2");
        db.Branches.AddRange(branch1, branch2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ZReadings.AddRange(ZReading(branch1.Id, 1), ZReading(branch2.Id, 1));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllZReadingsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllZReadingsQuery(branch1.Id), TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("POS_CLOSING", FormAction.View, "br-1", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllZReadingsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllZReadingsQuery("br-1"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
