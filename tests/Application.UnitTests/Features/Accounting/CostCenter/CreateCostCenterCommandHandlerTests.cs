namespace ZARI.Application.UnitTests.Features.Accounting.CostCenter;

using ZARI.Application.Features.Accounting.CostCenters.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateCostCenterCommandHandlerTests
{
    private static CreateCostCenterCommand Command(string? branchId = null, string code = "CC1") => new(branchId, code, "Test Cost Center", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_CostCenter_Without_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_CostCenter_With_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_Without_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("COST_CENTERS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCostCenterCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_With_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("COST_CENTERS", FormAction.Create, branch.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCostCenterCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.CostCenters.Add(LoanTestFixtures.CostCenter(code: "CC1"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCostCenterCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
    }
}
