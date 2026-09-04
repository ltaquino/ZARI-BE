namespace ZARI.Application.Features.Reporting.Datasets.Get;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Domain.Common;

/// <summary>Catalog for the designer's dataset picker — only datasets whose RequiredPermissionCode
/// the current user actually holds are included, so nobody can even see a dataset name for data
/// they can't access.</summary>
public sealed class GetReportDatasetsQueryHandler(
    IPermissionService permissionService,
    IEnumerable<IReportDataset> datasets) : IQueryHandler<GetReportDatasetsQuery, Result<List<ReportDatasetResponse>>>
{
    public async Task<Result<List<ReportDatasetResponse>>> HandleAsync(GetReportDatasetsQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<ReportDatasetResponse>();

        foreach (var dataset in datasets)
        {
            if (!await permissionService.HasPermissionAsync(dataset.RequiredPermissionCode, FormAction.View, cancellationToken))
                continue;

            var fields = dataset.Fields
                .Select(f => new ReportDatasetFieldResponse(f.Key, f.Label, f.Type.ToString(), f.Filterable, f.Sortable))
                .ToList();

            result.Add(new ReportDatasetResponse(dataset.Key, dataset.Label, fields));
        }

        return Result.Success(result);
    }
}
