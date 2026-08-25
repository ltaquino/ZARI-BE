namespace ZARI.Application.Features.Purchasing.ApInvoices.Approve;

using FluentValidation;

public sealed class ApproveApInvoiceValidator : AbstractValidator<ApproveApInvoiceCommand>
{
    public ApproveApInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
