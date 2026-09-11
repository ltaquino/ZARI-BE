namespace ZARI.Application.Features.Loan.LoanWriteOffs.Create;

using FluentValidation;

public sealed class CreateLoanWriteOffValidator : AbstractValidator<CreateLoanWriteOffCommand>
{
    public CreateLoanWriteOffValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.LoanAccountId).NotEmpty();
        RuleFor(x => x.WriteOffDate).NotEmpty();
        RuleFor(x => x.WriteOffExpenseAccountId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
