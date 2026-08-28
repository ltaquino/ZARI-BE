namespace ZARI.Application.Features.Accounting.ManualJournalEntries.RejectCancellation;

using FluentValidation;

public sealed class RejectManualJournalEntryCancellationValidator : AbstractValidator<RejectManualJournalEntryCancellationCommand>
{
    public RejectManualJournalEntryCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
