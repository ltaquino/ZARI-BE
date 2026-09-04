namespace ZARI.Application.Features.Reporting.ReportTemplates.Update;

using System.Text.Json;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using ZARI.Application.Features.Reporting.ReportTemplates.Shared;
using ZARI.Domain.Common;

public sealed class UpdateReportTemplateCommandHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IEnumerable<IReportDataset> datasets) : ICommandHandler<UpdateReportTemplateCommand>
{
    public async Task<Result> HandleAsync(UpdateReportTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.ReportTemplates.FindAsync([command.Id], cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("ReportTemplate.NotFound", $"Report template with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("REPORT_DESIGNER", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to update report templates."));

        // Simplest correct ownership rule: only the owner may update — no admin-override path yet.
        if (template.OwnerUserId != currentUser.UserId)
            return Result.Failure(Error.Forbidden("ReportTemplate.NotOwner", "Only the owner of this report template may update it."));

        var dataset = datasets.FirstOrDefault(d => d.Key == command.DatasetKey);
        if (dataset is null)
            return Result.Failure(Error.Validation("ReportTemplate.InvalidDataset", $"'{command.DatasetKey}' is not a recognized report dataset."));

        if (!await permissionService.HasPermissionAsync(dataset.RequiredPermissionCode, FormAction.View, cancellationToken))
            return Result.Failure(Error.Forbidden("ReportTemplate.Forbidden", "You do not have permission to view the underlying data for this dataset."));

        var validFieldKeys = dataset.Fields.Select(f => f.Key).ToHashSet();

        var unknownColumnKey = command.Columns.Select(c => c.FieldKey).FirstOrDefault(k => !validFieldKeys.Contains(k));
        if (unknownColumnKey is not null)
            return Result.Failure(Error.Validation("ReportTemplate.UnknownField", $"'{unknownColumnKey}' is not a field on dataset '{dataset.Key}'."));

        var unknownFilterKey = command.Filters.Select(f => f.FieldKey).FirstOrDefault(k => !validFieldKeys.Contains(k));
        if (unknownFilterKey is not null)
            return Result.Failure(Error.Validation("ReportTemplate.UnknownField", $"'{unknownFilterKey}' is not a field on dataset '{dataset.Key}'."));

        if (command.Sort is not null && !validFieldKeys.Contains(command.Sort.FieldKey))
            return Result.Failure(Error.Validation("ReportTemplate.UnknownField", $"'{command.Sort.FieldKey}' is not a field on dataset '{dataset.Key}'."));

        var groupByFieldKeys = command.GroupByFieldKeys ?? [];

        var groupByError = ReportTemplateGroupByValidator.Validate(groupByFieldKeys, command.Columns, validFieldKeys, dataset.Key);
        if (groupByError is not null)
            return Result.Failure(groupByError);

        var normalizedColumns = ReportTemplateGroupByValidator.NormalizeColumns(command.Columns, groupByFieldKeys);

        template.Name = command.Name;
        template.Description = command.Description;
        template.DatasetKey = command.DatasetKey;
        template.PaperSize = command.PaperSize;
        template.Orientation = command.Orientation;
        template.HeaderText = command.HeaderText;
        template.FooterText = command.FooterText;
        template.ShowColumnTotals = command.ShowColumnTotals;
        template.ColumnsJson = JsonSerializer.Serialize(normalizedColumns);
        template.FiltersJson = JsonSerializer.Serialize(command.Filters);
        template.SortJson = command.Sort is null ? null : JsonSerializer.Serialize(command.Sort);
        template.GroupByJson = JsonSerializer.Serialize(groupByFieldKeys);
        template.IsShared = command.IsShared;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
