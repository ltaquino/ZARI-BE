namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.Datasets.Values;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetReportFieldValuesQueryHandlerTests
{
    private static GetReportFieldValuesQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset dataset,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(), [dataset]);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dataset_Not_Recognized()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = Handler(db, new FakeReportDataset());

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("UNKNOWN", "Name", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.InvalidDataset");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Dataset_Permission()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FAKE_PERMISSION", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = Handler(db, new FakeReportDataset(), permissions: permissions);

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "Name", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Field_Not_On_Dataset()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = Handler(db, new FakeReportDataset());

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "NotAField", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.UnknownField");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Field_Not_Filterable()
    {
        await using var db = TestDbContextFactory.Create();
        var dataset = new FakeReportDataset(fields: [new ReportFieldDefinition("Locked", "Locked", ReportFieldType.Text, Filterable: false)]);
        var handler = Handler(db, dataset);

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "Locked", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.FieldNotFilterable");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Distinct_Sorted_Values()
    {
        await using var db = TestDbContextFactory.Create();
        var dataset = new FakeReportDataset(
            fields: [new ReportFieldDefinition("Name", "Name", ReportFieldType.Text)],
            run: _ => new ReportDatasetRunResult(
                [
                    new Dictionary<string, object?> { ["Name"] = "Charlie" },
                    new Dictionary<string, object?> { ["Name"] = "alpha" },
                    new Dictionary<string, object?> { ["Name"] = "Charlie" },
                    new Dictionary<string, object?> { ["Name"] = "" },
                ], false, 4));
        var handler = Handler(db, dataset);

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "Name", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal("alpha", "Charlie");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_Closed_When_User_Has_No_Branch_Assignments()
    {
        // FakeReportDataset's default Fields include a "BranchId" field, so ReportBranchScope
        // appends a branch-restricting filter unconditionally. With zero UserBranch rows, that
        // filter must resolve to a value matching no real branch, not "everything."
        await using var db = TestDbContextFactory.Create();
        var dataset = new FakeReportDataset();
        var handler = Handler(db, dataset);

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "Name", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        dataset.LastRequest!.Filters.Should().ContainSingle(f => f.FieldKey == "BranchId" && f.Value == "__NO_BRANCH_ASSIGNED__");
    }

    [Fact]
    public async Task HandleAsync_Should_Scope_To_Users_Assigned_Branches()
    {
        await using var db = TestDbContextFactory.Create();
        db.UserBranches.Add(new UserBranch { UserId = "user-1", BranchId = "br-1" });
        db.UserBranches.Add(new UserBranch { UserId = "user-1", BranchId = "br-2" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var dataset = new FakeReportDataset();
        var handler = Handler(db, dataset);

        var result = await handler.HandleAsync(new GetReportFieldValuesQuery("FAKE_DATASET", "Name", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        dataset.LastRequest!.Filters.Should().ContainSingle(f => f.FieldKey == "BranchId" && f.Value == "br-1,br-2");
    }
}
