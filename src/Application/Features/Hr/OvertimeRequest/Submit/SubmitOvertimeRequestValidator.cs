namespace ZARI.Application.Features.Hr.OvertimeRequests.Submit;

using FluentValidation;

public sealed class SubmitOvertimeRequestValidator : AbstractValidator<SubmitOvertimeRequestCommand>
{
    public SubmitOvertimeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
