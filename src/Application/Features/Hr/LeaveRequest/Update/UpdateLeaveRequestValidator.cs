namespace ZARI.Application.Features.Hr.LeaveRequests.Update;

using FluentValidation;

public sealed class UpdateLeaveRequestValidator : AbstractValidator<UpdateLeaveRequestCommand>
{
    public UpdateLeaveRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom);
        RuleFor(x => x.Reason).MaximumLength(300);
        RuleFor(x => x.AttachmentUrl).MaximumLength(300);
    }
}
