namespace ZARI.Application.Features.Accounting.BankAccounts.Create;

using FluentValidation;

public sealed class CreateBankAccountValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(25);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GlAccountId).NotEmpty();
        RuleFor(x => x.CurrencyId).MaximumLength(25);
    }
}
