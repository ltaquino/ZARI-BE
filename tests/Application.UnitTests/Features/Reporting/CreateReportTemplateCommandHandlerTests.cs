namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Create;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateReportTemplateCommandHandlerTests
{
    private static CreateReportTemplateCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset? dataset = null,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(),
            [dataset ?? new FakeReportDataset()]);

    private static CreateReportTemplateCommand Command(
        List<ReportTemplateColumn>? columns = null,
        List<ReportTemplateFilter>? filters = null,
        ReportTemplateSort? sort = null,
        List<string>? groupByFieldKeys = null) =>
        new("Test Report", "desc", "FAKE_DATASET", "A4", "Portrait", null, null, false, true,
            columns ?? [new ReportTemplateColumn("Name", "Name", 0)],
            filters ?? [],
            sort,
            groupByFieldKeys ?? []);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Report_Designer()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("REPORT_DESIGNER", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions: permissions).HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dataset_Not_Recognized()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command() with { DatasetKey = "UNKNOWN" }, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.InvalidDataset");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Dataset_Permission()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FAKE_PERMISSION", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions: permissions).HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Column_References_Unknown_Field()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(columns: [new ReportTemplateColumn("NotAField", "Bad", 0)]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.UnknownField");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Filter_References_Unknown_Field()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(filters: [new ReportTemplateFilter("NotAField", ReportFilterOperator.Equals, "x", null, false)]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.UnknownField");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Sort_References_Unknown_Field()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(sort: new ReportTemplateSort("NotAField", false)), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.UnknownField");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_GroupBy_Field_Not_Selected_As_Column()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(groupByFieldKeys: ["BranchId"]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.GroupByFieldNotSelected");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Non_GroupBy_Column_Missing_Aggregate()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(
            columns: [new ReportTemplateColumn("Name", "Name", 0), new ReportTemplateColumn("Amount", "Amount", 1)],
            groupByFieldKeys: ["Name"]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.MissingAggregate");
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Template_And_Clear_Aggregate_On_GroupBy_Column()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(Command(
            columns: [new ReportTemplateColumn("Name", "Name", 0, ReportAggregateFunction.Count), new ReportTemplateColumn("Amount", "Amount", 1, ReportAggregateFunction.Sum)],
            groupByFieldKeys: ["Name"]), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DatasetLabel.Should().Be("Fake Dataset");
        result.Value.OwnerUserId.Should().Be("user-1");
        result.Value.IsOwner.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(c => c.FieldKey == "Name" && c.Aggregate == null);
        result.Value.Columns.Should().ContainSingle(c => c.FieldKey == "Amount" && c.Aggregate == ReportAggregateFunction.Sum);
        db.ReportTemplates.Should().ContainSingle();
    }
}
