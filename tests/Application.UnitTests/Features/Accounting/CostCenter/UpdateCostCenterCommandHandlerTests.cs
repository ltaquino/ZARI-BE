namespace ZARI.Application.UnitTests.Features.Accounting.CostCenter;

using ZARI.Application.Features.Accounting.CostCenters.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateCostCenterCommandHandlerTests
{
    private static UpdateCostCenterCommand Command(Guid id, string? branchId = null, string code = "CC1") => new(id, branchId, code, "Updated Name", "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_CostCenter()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter(code: "CC1");
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(costCenter.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.CostCenters.FindAsync([costCenter.Id], TestContext.Current.CancellationToken))!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter(code: "CC1");
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("COST_CENTERS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateCostCenterCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(costCenter.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter(code: "CC1");
        var other = LoanTestFixtures.CostCenter(code: "CC2");
        db.CostCenters.AddRange(costCenter, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(costCenter.Id, code: "CC2"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var costCenter = LoanTestFixtures.CostCenter(code: "CC1");
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(costCenter.Id, branchId: "br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}
