namespace ZARI.Application.Features.Hr.LeaveRequests.Create;

using FluentValidation;

public sealed class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom);
        RuleFor(x => x.Reason).MaximumLength(300);
        RuleFor(x => x.AttachmentUrl).MaximumLength(300);
    }
}
