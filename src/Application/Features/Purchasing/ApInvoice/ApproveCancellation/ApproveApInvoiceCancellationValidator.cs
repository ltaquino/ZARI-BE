namespace ZARI.Application.Features.Purchasing.ApInvoices.ApproveCancellation;

using FluentValidation;

public sealed class ApproveApInvoiceCancellationValidator : AbstractValidator<ApproveApInvoiceCancellationCommand>
{
    public ApproveApInvoiceCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
