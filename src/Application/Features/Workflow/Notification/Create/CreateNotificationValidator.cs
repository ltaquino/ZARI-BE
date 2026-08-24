namespace ZARI.Application.Features.Workflow.Notifications.Create;

using FluentValidation;

public sealed class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(25);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ActorUserId).MaximumLength(150);
    }
}
