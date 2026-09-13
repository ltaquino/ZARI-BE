namespace ZARI.Application.Features.Hr.PayrollRuns.Approve;

using FluentValidation;

public sealed class ApprovePayrollRunValidator : AbstractValidator<ApprovePayrollRunCommand>
{
    public ApprovePayrollRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
