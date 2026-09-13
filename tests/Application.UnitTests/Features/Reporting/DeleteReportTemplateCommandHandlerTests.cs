namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.ReportTemplates.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteReportTemplateCommandHandlerTests
{
    private static DeleteReportTemplateCommandHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser());

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
        var result = await Handler(db).HandleAsync(new DeleteReportTemplateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, template) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("REPORT_DESIGNER", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new DeleteReportTemplateCommand(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Owner()
    {
        var (db, template) = await Seed(ownerUserId: "other-user");

        var result = await Handler(db).HandleAsync(new DeleteReportTemplateCommand(template.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ReportTemplate.NotOwner");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Owned_Template()
    {
        var (db, template) = await Seed();

        var result = await Handler(db).HandleAsync(new DeleteReportTemplateCommand(template.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.ReportTemplates.Should().BeEmpty();
        await db.DisposeAsync();
    }
}
