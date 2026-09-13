namespace ZARI.Application.Features.Hr.OvertimeRequests.Approve;

using FluentValidation;

public sealed class ApproveOvertimeRequestValidator : AbstractValidator<ApproveOvertimeRequestCommand>
{
    public ApproveOvertimeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
