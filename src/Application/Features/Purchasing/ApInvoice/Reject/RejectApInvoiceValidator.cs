namespace ZARI.Application.Features.Purchasing.ApInvoices.Reject;

using FluentValidation;

public sealed class RejectApInvoiceValidator : AbstractValidator<RejectApInvoiceCommand>
{
    public RejectApInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
