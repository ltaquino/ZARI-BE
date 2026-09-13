namespace ZARI.Application.Features.Loan.LoanWriteOffs.Update;

using FluentValidation;

public sealed class UpdateLoanWriteOffValidator : AbstractValidator<UpdateLoanWriteOffCommand>
{
    public UpdateLoanWriteOffValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WriteOffDate).NotEmpty();
        RuleFor(x => x.WriteOffExpenseAccountId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
