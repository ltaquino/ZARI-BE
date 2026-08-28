namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Cancel;

using FluentValidation;

public sealed class CancelManualJournalEntryValidator : AbstractValidator<CancelManualJournalEntryCommand>
{
    public CancelManualJournalEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
