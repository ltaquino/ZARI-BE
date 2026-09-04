namespace ZARI.Application.Features.Reporting.ReportTemplates.Create;

using System.Text.Json;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Get;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateReportTemplateCommandHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEnumerable<IReportDataset> datasets) : ICommandHandler<CreateReportTemplateCommand, Result<ReportTemplateDetailResponse>>
{
    public async Task<Result<ReportTemplateDetailResponse>> HandleAsync(CreateReportTemplateCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.Create, cancellationToken))
            return Result.Failure<ReportTemplateDetailResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to create report templates."));

        var dataset = datasets.FirstOrDefault(d => d.Key == command.DatasetKey);
        if (dataset is null)
            return Result.Failure<ReportTemplateDetailResponse>(Error.Validation("ReportTemplate.InvalidDataset", $"'{command.DatasetKey}' is not a recognized report dataset."));

        // Defense in depth: REPORT_DESIGNER access alone shouldn't let someone build a report over
        // data they can't otherwise see — the dataset's own permission must also be held.
        if (!await permissionService.HasPermissionAsync(dataset.RequiredPermissionCode, FormAction.View, cancellationToken))
            return Result.Failure<ReportTemplateDetailResponse>(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view the underlying data for this dataset."));

        var validFieldKeys = dataset.Fields.Select(f => f.Key).ToHashSet();

        var unknownColumnKey = command.Columns.Select(c => c.FieldKey).FirstOrDefault(k => !validFieldKeys.Contains(k));
        if (unknownColumnKey is not null)
            return Result.Failure<ReportTemplateDetailResponse>(Error.Validation("ReportTemplate.UnknownField", $"'{unknownColumnKey}' is not a field on dataset '{dataset.Key}'."));

        var unknownFilterKey = command.Filters.Select(f => f.FieldKey).FirstOrDefault(k => !validFieldKeys.Contains(k));
        if (unknownFilterKey is not null)
            return Result.Failure<ReportTemplateDetailResponse>(Error.Validation("ReportTemplate.UnknownField", $"'{unknownFilterKey}' is not a field on dataset '{dataset.Key}'."));

        if (command.Sort is not null && !validFieldKeys.Contains(command.Sort.FieldKey))
            return Result.Failure<ReportTemplateDetailResponse>(Error.Validation("ReportTemplate.UnknownField", $"'{command.Sort.FieldKey}' is not a field on dataset '{dataset.Key}'."));

        var groupByFieldKeys = command.GroupByFieldKeys ?? [];

        var groupByError = ReportTemplateGroupByValidator.Validate(groupByFieldKeys, command.Columns, validFieldKeys, dataset.Key);
        if (groupByError is not null)
            return Result.Failure<ReportTemplateDetailResponse>(groupByError);

        var normalizedColumns = ReportTemplateGroupByValidator.NormalizeColumns(command.Columns, groupByFieldKeys);

        var template = new ReportTemplate
        {
            Name = command.Name,
            Description = command.Description,
            DatasetKey = command.DatasetKey,
            PaperSize = command.PaperSize,
            Orientation = command.Orientation,
            HeaderText = command.HeaderText,
            FooterText = command.FooterText,
            ShowColumnTotals = command.ShowColumnTotals,
            ColumnsJson = JsonSerializer.Serialize(normalizedColumns),
            FiltersJson = JsonSerializer.Serialize(command.Filters),
            SortJson = command.Sort is null ? null : JsonSerializer.Serialize(command.Sort),
            GroupByJson = JsonSerializer.Serialize(groupByFieldKeys),
            IsShared = command.IsShared,
            OwnerUserId = currentUser.UserId!,
            Status = "Active"
        };

        dbContext.ReportTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ReportTemplateDetailResponse(
            template.Id,
            template.Name,
            template.Description,
            template.DatasetKey,
            dataset.Label,
            template.PaperSize,
            template.Orientation,
            template.HeaderText,
            template.FooterText,
            template.ShowColumnTotals,
            normalizedColumns,
            command.Filters,
            command.Sort,
            groupByFieldKeys,
            template.IsShared,
            template.OwnerUserId,
            true,
            template.CreatedAt,
            template.LastModifiedAt);

        return Result.Success(response);
    }
}
