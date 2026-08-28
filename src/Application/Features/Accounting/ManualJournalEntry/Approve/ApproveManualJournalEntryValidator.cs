namespace ZARI.Application.Features.Accounting.ManualJournalEntries.Approve;

using FluentValidation;

public sealed class ApproveManualJournalEntryValidator : AbstractValidator<ApproveManualJournalEntryCommand>
{
    public ApproveManualJournalEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
