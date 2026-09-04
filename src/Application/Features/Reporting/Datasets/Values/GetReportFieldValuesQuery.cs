namespace ZARI.Application.Features.Reporting.Datasets.Values;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>Distinct known values for one filterable field of one dataset, optionally narrowed by
/// a Contains search — powers a searchable-dropdown filter-value picker in the designer/viewer so
/// a user picks a real value instead of typing one from memory.</summary>
public sealed record GetReportFieldValuesQuery(string DatasetKey, string FieldKey, string? Search) : IQuery<Result<List<string>>>;
