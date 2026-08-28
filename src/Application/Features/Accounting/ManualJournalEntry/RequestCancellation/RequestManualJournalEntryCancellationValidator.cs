namespace ZARI.Application.Features.Accounting.ManualJournalEntries.RequestCancellation;

using FluentValidation;

public sealed class RequestManualJournalEntryCancellationValidator : AbstractValidator<RequestManualJournalEntryCancellationCommand>
{
    public RequestManualJournalEntryCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
