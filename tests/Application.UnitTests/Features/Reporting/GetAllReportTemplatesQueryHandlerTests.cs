namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.ReportTemplates.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllReportTemplatesQueryHandlerTests
{
    private static GetAllReportTemplatesQueryHandler Handler(
        ZARI.Infrastructure.Persistence.AppDbContext db,
        FakeReportDataset? dataset = null,
        ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null,
        ZARI.Application.Abstractions.Identity.ICurrentUser? currentUser = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService(), currentUser ?? ReportingTestFixtures.CurrentUser(),
            [dataset ?? new FakeReportDataset()]);

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("REPORT_DESIGNER", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions: permissions).HandleAsync(new GetAllReportTemplatesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Own_And_Shared_Templates_Only()
    {
        await using var db = TestDbContextFactory.Create();
        var mine = ReportingTestFixtures.ReportTemplate(ownerUserId: "user-1", isShared: false);
        mine.Name = "Mine";
        var sharedByOther = ReportingTestFixtures.ReportTemplate(ownerUserId: "other-user", isShared: true);
        sharedByOther.Name = "Shared";
        var privateOfOther = ReportingTestFixtures.ReportTemplate(ownerUserId: "other-user", isShared: false);
        privateOfOther.Name = "Hidden";
        db.ReportTemplates.AddRange(mine, sharedByOther, privateOfOther);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new GetAllReportTemplatesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(t => t.Name == "Mine" && t.IsOwner);
        result.Value.Should().Contain(t => t.Name == "Shared" && !t.IsOwner);
        result.Value.Should().NotContain(t => t.Name == "Hidden");
    }
}
