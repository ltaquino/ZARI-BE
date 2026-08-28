namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Update;

using FluentValidation;

public sealed class UpdateManualJournalEntryValidator : AbstractValidator<UpdateManualJournalEntryCommand>
{
    public UpdateManualJournalEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(300);

        RuleFor(x => x.Lines).Must(l => l.Count >= 2).WithMessage("At least two lines are required for a balanced entry.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.GlAccountId).NotEmpty();
            line.RuleFor(l => l.Memo).MaximumLength(300);
            line.RuleFor(l => l.DebitAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l).Must(l => l.DebitAmount > 0 != l.CreditAmount > 0)
                .WithMessage("Each line needs either a debit or a credit amount — not both, not neither.");
        });

        RuleFor(x => x.Lines)
            .Must(lines => Math.Round(lines.Sum(l => l.DebitAmount), 4) == Math.Round(lines.Sum(l => l.CreditAmount), 4))
            .WithMessage("Total debits must equal total credits.")
            .When(x => x.Lines.Count > 0);
    }
}
