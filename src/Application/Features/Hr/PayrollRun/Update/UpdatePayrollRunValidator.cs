namespace ZARI.Application.Features.Hr.PayrollRuns.Update;

using FluentValidation;

public sealed class UpdatePayrollRunValidator : AbstractValidator<UpdatePayrollRunCommand>
{
    public UpdatePayrollRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PayPeriodEnd).GreaterThanOrEqualTo(x => x.PayPeriodStart);
        RuleFor(x => x.PayDate).GreaterThanOrEqualTo(x => x.PayPeriodEnd);
    }
}
