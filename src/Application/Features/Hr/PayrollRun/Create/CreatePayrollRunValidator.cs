namespace ZARI.Application.Features.Hr.PayrollRuns.Create;

using FluentValidation;

public sealed class CreatePayrollRunValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PayPeriodEnd).GreaterThanOrEqualTo(x => x.PayPeriodStart);
        RuleFor(x => x.PayDate).GreaterThanOrEqualTo(x => x.PayPeriodEnd);
    }
}
