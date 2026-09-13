namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Approve;

using FluentValidation;

public sealed class ApproveThirteenthMonthPayRunValidator : AbstractValidator<ApproveThirteenthMonthPayRunCommand>
{
    public ApproveThirteenthMonthPayRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
