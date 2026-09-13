namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.CreateAdjustment;

using FluentValidation;

public sealed class CreateLeaveBalanceAdjustmentValidator : AbstractValidator<CreateLeaveBalanceAdjustmentCommand>
{
    // USAGE is deliberately excluded — it's only ever posted automatically by
    // ApproveLeaveRequestCommandHandler, never through this manual endpoint.
    private static readonly string[] ValidTypes = ["ACCRUAL", "CARRYOVER", "ADJUSTMENT"];

    public CreateLeaveBalanceAdjustmentValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}.");
        RuleFor(x => x.Days).NotEqual(0);
    }
}
