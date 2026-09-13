namespace ZARI.Application.Features.Customers.CreditRecords.Create;

using FluentValidation;

public sealed class CreateCustomerCreditRecordValidator : AbstractValidator<CreateCustomerCreditRecordCommand>
{
    private static readonly string[] ValidRecordTypes =
        ["DEFAULT", "FORECLOSURE", "ADVERSE_JUDGMENT", "BANKRUPTCY", "BOUNCED_CHECK", "PENDING_LITIGATION", "OTHER"];

    public CreateCustomerCreditRecordValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.RecordType).NotEmpty().Must(t => ValidRecordTypes.Contains(t))
            .WithMessage($"Record type must be one of: {string.Join(", ", ValidRecordTypes)}.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RecordDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(300);
    }
}
