namespace ZARI.Application.Features.Hr.PayrollRuns.Reject;

using FluentValidation;

public sealed class RejectPayrollRunValidator : AbstractValidator<RejectPayrollRunCommand>
{
    public RejectPayrollRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
