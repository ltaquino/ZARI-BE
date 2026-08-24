namespace ZARI.Application.Features.Workflow.Notifications.MarkRead;

using FluentValidation;

public sealed class MarkNotificationsReadValidator : AbstractValidator<MarkNotificationsReadCommand>
{
    public MarkNotificationsReadValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Ids).NotNull();
    }
}
