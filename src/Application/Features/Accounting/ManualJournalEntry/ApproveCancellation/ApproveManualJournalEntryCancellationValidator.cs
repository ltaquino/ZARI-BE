namespace ZARI.Application.Features.Accounting.ManualJournalEntries.ApproveCancellation;

using FluentValidation;

public sealed class ApproveManualJournalEntryCancellationValidator : AbstractValidator<ApproveManualJournalEntryCancellationCommand>
{
    public ApproveManualJournalEntryCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
