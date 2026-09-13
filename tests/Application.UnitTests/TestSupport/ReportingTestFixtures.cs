namespace ZARI.Application.UnitTests.TestSupport;

using NSubstitute;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Domain.Entities;

/// <summary>
/// Test double for IReportDataset — the sweep's per-module count is "8 handlers" (the 5
/// ReportTemplate CRUD-ish handlers plus Run/GetDatasets/GetFieldValues), not the ~26 individual
/// dataset implementations (SalesInvoicesReportDataset etc.), which are internal plumbing consumed
/// by RunReportTemplateQueryHandler rather than their own ICommandHandler/IQueryHandler. This fake
/// lets the 8 real handlers be tested (including how they build ReportDatasetRunRequest and use its
/// result) without depending on any one dataset's real EF-translatable filter/sort logic.
/// </summary>
internal sealed class FakeReportDataset(
    string key = "FAKE_DATASET",
    string label = "Fake Dataset",
    string requiredPermissionCode = "FAKE_PERMISSION",
    IReadOnlyList<ReportFieldDefinition>? fields = null,
    Func<ReportDatasetRunRequest, ReportDatasetRunResult>? run = null) : IReportDataset
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string RequiredPermissionCode { get; } = requiredPermissionCode;

    public IReadOnlyList<ReportFieldDefinition> Fields { get; } = fields ??
    [
        new ReportFieldDefinition("Name", "Name", ReportFieldType.Text),
        new ReportFieldDefinition("Amount", "Amount", ReportFieldType.Currency),
        new ReportFieldDefinition("BranchId", "Branch", ReportFieldType.Text)
    ];

    public ReportDatasetRunRequest? LastRequest { get; private set; }

    public Task<ReportDatasetRunResult> RunAsync(IAppDbContext dbContext, ReportDatasetRunRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(run?.Invoke(request) ?? new ReportDatasetRunResult([], false, 0));
    }
}

internal static class ReportingTestFixtures
{
    public static ICurrentUser CurrentUser(string userId = "user-1")
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(userId);
        return currentUser;
    }

    public static ReportTemplate ReportTemplate(
        string datasetKey = "FAKE_DATASET",
        string ownerUserId = "user-1",
        bool isShared = false,
        string columnsJson = "[]",
        string filtersJson = "[]",
        string? sortJson = null,
        string groupByJson = "[]") => new()
    {
        Name = "Test Report",
        DatasetKey = datasetKey,
        PaperSize = "A4",
        Orientation = "Portrait",
        ColumnsJson = columnsJson,
        FiltersJson = filtersJson,
        SortJson = sortJson,
        GroupByJson = groupByJson,
        IsShared = isShared,
        OwnerUserId = ownerUserId,
        Status = "Active"
    };
}
