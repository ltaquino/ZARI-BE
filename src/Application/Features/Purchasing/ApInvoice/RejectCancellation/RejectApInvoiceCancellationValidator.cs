namespace ZARI.Application.Features.Purchasing.ApInvoices.RejectCancellation;

using FluentValidation;

public sealed class RejectApInvoiceCancellationValidator : AbstractValidator<RejectApInvoiceCancellationCommand>
{
    public RejectApInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
