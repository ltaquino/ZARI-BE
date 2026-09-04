namespace ZARI.Application.Features.Reporting.Datasets.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetReportDatasetsQuery : IQuery<Result<List<ReportDatasetResponse>>>;

public sealed record ReportDatasetFieldResponse(string Key, string Label, string Type, bool Filterable, bool Sortable);

public sealed record ReportDatasetResponse(string Key, string Label, List<ReportDatasetFieldResponse> Fields);
