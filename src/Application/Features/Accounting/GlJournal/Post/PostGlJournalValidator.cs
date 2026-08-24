namespace ZARI.Application.Features.Accounting.GlJournals.Post;

using FluentValidation;

public sealed class PostGlJournalValidator : AbstractValidator<PostGlJournalCommand>
{
    public PostGlJournalValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.JournalDate).NotEmpty();
        RuleFor(x => x.SourceReferenceTable).NotEmpty().MaximumLength(25);
        RuleFor(x => x.SourceReferenceId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A journal must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.DebitAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Memo).MaximumLength(300);
        });
    }
}
