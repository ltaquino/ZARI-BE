namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Submit;

using FluentValidation;

public sealed class SubmitManualJournalEntryValidator : AbstractValidator<SubmitManualJournalEntryCommand>
{
    public SubmitManualJournalEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
