namespace ZARI.Application.Features.Hr.PayrollRuns.Submit;

using FluentValidation;

public sealed class SubmitPayrollRunValidator : AbstractValidator<SubmitPayrollRunCommand>
{
    public SubmitPayrollRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
