namespace ZARI.Application.Features.Reporting.ReportTemplates.Create;

using FluentValidation;

public sealed class CreateReportTemplateValidator : AbstractValidator<CreateReportTemplateCommand>
{
    private static readonly string[] ValidPaperSizes = ["A4", "Letter", "Legal"];
    private static readonly string[] ValidOrientations = ["Portrait", "Landscape"];

    public CreateReportTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DatasetKey).NotEmpty().MaximumLength(100);

        RuleFor(x => x.PaperSize)
            .NotEmpty()
            .Must(s => ValidPaperSizes.Contains(s))
            .WithMessage($"Paper size must be one of: {string.Join(", ", ValidPaperSizes)}.");

        RuleFor(x => x.Orientation)
            .NotEmpty()
            .Must(s => ValidOrientations.Contains(s))
            .WithMessage($"Orientation must be one of: {string.Join(", ", ValidOrientations)}.");

        RuleFor(x => x.Columns)
            .NotEmpty().WithMessage("At least one column must be selected.")
            .Must(cols => cols.All(c => !string.IsNullOrWhiteSpace(c.FieldKey) && !string.IsNullOrWhiteSpace(c.Label)))
            .WithMessage("Every column must have a field key and a label.");

        RuleFor(x => x.Filters)
            .Must(filters => filters.All(f => !string.IsNullOrWhiteSpace(f.FieldKey)))
            .WithMessage("Every filter must reference a field key.");
    }
}
