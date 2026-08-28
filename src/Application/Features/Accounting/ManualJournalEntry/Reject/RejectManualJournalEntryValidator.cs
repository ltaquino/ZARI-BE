namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Reject;

using FluentValidation;

public sealed class RejectManualJournalEntryValidator : AbstractValidator<RejectManualJournalEntryCommand>
{
    public RejectManualJournalEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
