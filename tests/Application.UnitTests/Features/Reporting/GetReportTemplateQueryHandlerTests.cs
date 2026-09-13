namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.ReportTemplates.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetReportTemplateQueryHandlerTests
{
    private static GetReportTemplateQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset? dataset = null,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(),
            [dataset ?? new FakeReportDataset()]);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ReportTemplate template)> Seed(bool isShared = false, string ownerUserId = "user-1")
    {
        var db = TestDbContextFactory.Create();
        var template = ReportingTestFixtures.ReportTemplate(ownerUserId: ownerUserId, isShared: isShared,
            columnsJson: """[{"FieldKey":"Name","Label":"Name","Order":0,"Aggregate":null}]""");
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

        var result = await Handler(db, permissions: permissions).HandleAsync(new GetReportTemplateQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(new GetReportTemplateQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Shared_And_Not_Owner()
    {
        var (db, template) = await Seed(isShared: false, ownerUserId: "other-user");

        var result = await Handler(db).HandleAsync(new GetReportTemplateQuery(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_Shared_Even_If_Not_Owner()
    {
        var (db, template) = await Seed(isShared: true, ownerUserId: "other-user");

        var result = await Handler(db).HandleAsync(new GetReportTemplateQuery(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsOwner.Should().BeFalse();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Owned_Template_With_Deserialized_Columns()
    {
        var (db, template) = await Seed();

        var result = await Handler(db).HandleAsync(new GetReportTemplateQuery(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsOwner.Should().BeTrue();
        result.Value.DatasetLabel.Should().Be("Fake Dataset");
        result.Value.Columns.Should().ContainSingle(c => c.FieldKey == "Name");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fall_Back_To_Dataset_Key_When_Dataset_Missing()
    {
        var (db, template) = await Seed();

        var result = await Handler(db, dataset: new FakeReportDataset(key: "SOME_OTHER_KEY")).HandleAsync(new GetReportTemplateQuery(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DatasetLabel.Should().Be("FAKE_DATASET");
        await db.DisposeAsync();
    }
}
