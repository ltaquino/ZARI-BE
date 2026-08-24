namespace ZARI.Application.Features.Workflow.ApprovalRequests.Decide;

using FluentValidation;

public sealed class DecideApprovalRequestValidator : AbstractValidator<DecideApprovalRequestCommand>
{
    public DecideApprovalRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).MaximumLength(300);

        RuleFor(x => x.Action)
            .Must(a => a is "Approve" or "Reject")
            .WithMessage("Action must be Approve or Reject.");
    }
}
