namespace ZARI.Application.Features.Accounting.GlAccounts.Update;

using FluentValidation;

public sealed class UpdateGlAccountValidator : AbstractValidator<UpdateGlAccountCommand>
{
    private static readonly string[] ValidAccountTypes = ["Asset", "Liability", "Equity", "Revenue", "Expense", "Cogs"];
    private static readonly string[] ValidNormalBalances = ["Debit", "Credit"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateGlAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x.AccountType)
            .NotEmpty()
            .Must(t => ValidAccountTypes.Contains(t))
            .WithMessage($"Account type must be one of: {string.Join(", ", ValidAccountTypes)}.");

        RuleFor(x => x.NormalBalance)
            .NotEmpty()
            .Must(b => ValidNormalBalances.Contains(b))
            .WithMessage($"Normal balance must be one of: {string.Join(", ", ValidNormalBalances)}.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
