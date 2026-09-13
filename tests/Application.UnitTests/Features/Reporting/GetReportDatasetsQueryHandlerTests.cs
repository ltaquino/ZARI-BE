namespace ZARI.Application.UnitTests.Features.Reporting;

using ZARI.Application.Features.Reporting.Datasets.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetReportDatasetsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Only_Include_Datasets_The_Caller_Has_Permission_For()
    {
        var visible = new FakeReportDataset(key: "VISIBLE", label: "Visible", requiredPermissionCode: "VISIBLE_PERM");
        var hidden = new FakeReportDataset(key: "HIDDEN", label: "Hidden", requiredPermissionCode: "HIDDEN_PERM");
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("HIDDEN_PERM", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetReportDatasetsQueryHandler(permissions, [visible, hidden]);

        var result = await handler.HandleAsync(new GetReportDatasetsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Key == "VISIBLE");
        result.Value.Should().NotContain(d => d.Key == "HIDDEN");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Datasets_Registered()
    {
        var handler = new GetReportDatasetsQueryHandler(LoanTestFixtures.AllowAllPermissionService(), []);

        var result = await handler.HandleAsync(new GetReportDatasetsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Include_Field_Definitions()
    {
        var dataset = new FakeReportDataset();
        var handler = new GetReportDatasetsQueryHandler(LoanTestFixtures.AllowAllPermissionService(), [dataset]);

        var result = await handler.HandleAsync(new GetReportDatasetsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Fields.Should().Contain(f => f.Key == "Name");
    }
}
