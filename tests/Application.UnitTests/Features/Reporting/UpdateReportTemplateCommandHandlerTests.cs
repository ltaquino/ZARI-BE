namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Application.Features.Reporting.ReportTemplates.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateReportTemplateCommandHandlerTests
{
    private static UpdateReportTemplateCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset? dataset = null,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(),
            [dataset ?? new FakeReportDataset()]);

    private static UpdateReportTemplateCommand Command(Guid id, List<ReportTemplateColumn>? columns = null) =>
        new(id, "Renamed Report", "desc", "FAKE_DATASET", "Letter", "Landscape", null, null, true, true,
            columns ?? [new ReportTemplateColumn("Name", "Name", 0)], [], null, []);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ReportTemplate template)> Seed(string ownerUserId = "user-1")
    {
        var db = TestDbContextFactory.Create();
        var template = ReportingTestFixtures.ReportTemplate(ownerUserId: ownerUserId);
        db.ReportTemplates.Add(template);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, template);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Report_Designer()
    {
        var (db, template) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("REPORT_DESIGNER", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions: permissions).HandleAsync(Command(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Owner()
    {
        var (db, template) = await Seed(ownerUserId: "other-user");

        var result = await Handler(db).HandleAsync(Command(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.NotOwner");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Dataset_Not_Recognized()
    {
        var (db, template) = await Seed();

        var result = await Handler(db).HandleAsync(Command(template.Id) with { DatasetKey = "UNKNOWN" }, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.InvalidDataset");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden_On_Dataset_Permission()
    {
        var (db, template) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FAKE_PERMISSION", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions: permissions).HandleAsync(Command(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Column_References_Unknown_Field()
    {
        var (db, template) = await Seed();

        var result = await Handler(db).HandleAsync(Command(template.Id, columns: [new ReportTemplateColumn("NotAField", "Bad", 0)]), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.UnknownField");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Owned_Template()
    {
        var (db, template) = await Seed();

        var result = await Handler(db).HandleAsync(Command(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.ReportTemplates.FindAsync([template.Id], TestContext.Current.CancellationToken);
        updated!.Name.Should().Be("Renamed Report");
        updated.PaperSize.Should().Be("Letter");
        updated.Orientation.Should().Be("Landscape");
        await db.DisposeAsync();
    }
}
