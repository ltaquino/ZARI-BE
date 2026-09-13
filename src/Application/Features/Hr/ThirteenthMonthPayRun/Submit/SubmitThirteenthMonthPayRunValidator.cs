namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Submit;

using FluentValidation;

public sealed class SubmitThirteenthMonthPayRunValidator : AbstractValidator<SubmitThirteenthMonthPayRunCommand>
{
    public SubmitThirteenthMonthPayRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
