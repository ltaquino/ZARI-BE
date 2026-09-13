namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Run;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class RunReportTemplateQueryHandlerTests
{
    private static RunReportTemplateQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset dataset,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(), [dataset]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ReportTemplate template)> Seed(
        string columnsJson, string filtersJson = "[]", string? sortJson = null, string groupByJson = "[]",
        bool isShared = false, string ownerUserId = "user-1", string datasetKey = "FAKE_DATASET")
    {
        var db = TestDbContextFactory.Create();
        var template = ReportingTestFixtures.ReportTemplate(datasetKey: datasetKey, ownerUserId: ownerUserId, isShared: isShared,
            columnsJson: columnsJson, filtersJson: filtersJson, sortJson: sortJson, groupByJson: groupByJson);
        db.ReportTemplates.Add(template);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, template);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Report_Designer()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("REPORT_DESIGNER", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, new FakeReportDataset(), permissions: permissions).HandleAsync(new RunReportTemplateQuery(Guid.NewGuid(), []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db, new FakeReportDataset()).HandleAsync(new RunReportTemplateQuery(Guid.NewGuid(), []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Shared_And_Not_Owner()
    {
        var (db, template) = await Seed("[]", ownerUserId: "other-user", isShared: false);

        var result = await Handler(db, new FakeReportDataset()).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dataset_No_Longer_Available()
    {
        var (db, template) = await Seed("[]", datasetKey: "GONE_DATASET");

        var result = await Handler(db, new FakeReportDataset(key: "FAKE_DATASET")).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.DatasetMissing");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Dataset_Permission()
    {
        var (db, template) = await Seed("[]");
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FAKE_PERMISSION", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, new FakeReportDataset(), permissions: permissions).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Run_Detail_Mode_Report()
    {
        var (db, template) = await Seed("""[{"FieldKey":"Name","Label":"Name","Order":0,"Aggregate":null}]""");
        var dataset = new FakeReportDataset(fields: [new ReportFieldDefinition("Name", "Name", ReportFieldType.Text, Filterable: true, Sortable: true)],
            run: _ => new ReportDatasetRunResult([new Dictionary<string, object?> { ["Name"] = "Row A" }], false, 1));

        var result = await Handler(db, dataset).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Columns.Should().ContainSingle(c => c.FieldKey == "Name" && c.Label == "Name");
        result.Value.Rows.Should().ContainSingle(r => (string)r["Name"]! == "Row A");
        result.Value.TotalRows.Should().Be(1);
        dataset.LastRequest!.ColumnKeys.Should().Equal("Name");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Pass_Null_For_Unsupplied_Prompted_Filter()
    {
        var (db, template) = await Seed(
            """[{"FieldKey":"Name","Label":"Name","Order":0,"Aggregate":null}]""",
            filtersJson: """[{"FieldKey":"Name","Operator":"Equals","Value":"seeded","Value2":null,"PromptAtRuntime":true}]""");
        var dataset = new FakeReportDataset();

        var result = await Handler(db, dataset).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        dataset.LastRequest!.Filters.Should().ContainSingle(f => f.FieldKey == "Name" && f.Value == null);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Runtime_Override_For_Prompted_Filter()
    {
        var (db, template) = await Seed(
            """[{"FieldKey":"Name","Label":"Name","Order":0,"Aggregate":null}]""",
            filtersJson: """[{"FieldKey":"Name","Operator":"Equals","Value":null,"Value2":null,"PromptAtRuntime":true}]""");
        var dataset = new FakeReportDataset();

        var result = await Handler(db, dataset).HandleAsync(
            new RunReportTemplateQuery(template.Id, [new RunReportTemplateFilterOverride("Name", "runtime-value", null)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        dataset.LastRequest!.Filters.Should().ContainSingle(f => f.FieldKey == "Name" && f.Value == "runtime-value");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Aggregate_Grouped_Report()
    {
        var (db, template) = await Seed(
            """[{"FieldKey":"Name","Label":"Name","Order":0,"Aggregate":null},{"FieldKey":"Amount","Label":"Amount","Order":1,"Aggregate":"Sum"}]""",
            groupByJson: """["Name"]""");
        var dataset = new FakeReportDataset(run: _ => new ReportDatasetRunResult(
            [
                new Dictionary<string, object?> { ["Name"] = "A", ["Amount"] = 10m },
                new Dictionary<string, object?> { ["Name"] = "A", ["Amount"] = 5m },
                new Dictionary<string, object?> { ["Name"] = "B", ["Amount"] = 3m },
            ], false, 3));

        var result = await Handler(db, dataset).HandleAsync(new RunReportTemplateQuery(template.Id, []), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalRows.Should().Be(2);
        result.Value.Rows.Should().ContainSingle(r => (string)r["Name"]! == "A" && (decimal)r["Amount"]! == 15m);
        result.Value.Rows.Should().ContainSingle(r => (string)r["Name"]! == "B" && (decimal)r["Amount"]! == 3m);
        // Grouped mode requests every group-by key from the dataset even though it's also a column.
        dataset.LastRequest!.ColumnKeys.Should().Contain("Name");
        dataset.LastRequest.RowCap.Should().Be(200_000);
        await db.DisposeAsync();
    }
}
