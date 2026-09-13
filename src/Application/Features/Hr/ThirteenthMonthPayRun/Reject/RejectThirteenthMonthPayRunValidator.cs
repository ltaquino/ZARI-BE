namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Reject;

using FluentValidation;

public sealed class RejectThirteenthMonthPayRunValidator : AbstractValidator<RejectThirteenthMonthPayRunCommand>
{
    public RejectThirteenthMonthPayRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
