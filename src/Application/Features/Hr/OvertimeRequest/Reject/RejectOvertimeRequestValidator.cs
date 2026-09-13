namespace ZARI.Application.Features.Hr.OvertimeRequests.Reject;

using FluentValidation;

public sealed class RejectOvertimeRequestValidator : AbstractValidator<RejectOvertimeRequestCommand>
{
    public RejectOvertimeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
