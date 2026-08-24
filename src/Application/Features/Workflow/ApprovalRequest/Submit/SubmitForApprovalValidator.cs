namespace ZARI.Application.Features.Workflow.ApprovalRequests.Submit;

using FluentValidation;

public sealed class SubmitForApprovalValidator : AbstractValidator<SubmitForApprovalCommand>
{
    private static readonly string[] ValidRequestTypes = ["SUBMIT", "CANCEL"];

    public SubmitForApprovalValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(25);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).MaximumLength(300);

        RuleFor(x => x.RequestType)
            .Must(t => t is null || ValidRequestTypes.Contains(t))
            .WithMessage($"Request type must be one of: {string.Join(", ", ValidRequestTypes)}.");
    }
}
