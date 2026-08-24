namespace ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;

using FluentValidation;

public sealed class CancelPendingApprovalRequestValidator : AbstractValidator<CancelPendingApprovalRequestCommand>
{
    public CancelPendingApprovalRequestValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(25);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(150);
    }
}
