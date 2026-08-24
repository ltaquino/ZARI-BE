namespace ZARI.Application.Features.Accounting.GlJournals.Reverse;

using FluentValidation;

public sealed class ReverseGlJournalsValidator : AbstractValidator<ReverseGlJournalsCommand>
{
    public ReverseGlJournalsValidator()
    {
        RuleFor(x => x.SourceReferenceTable).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SourceReferenceId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.JournalDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(300);
    }
}
