namespace ZARI.Application.Features.Inventory.SerialNumbers.Issue;

using FluentValidation;

public sealed class IssueSerialValidator : AbstractValidator<IssueSerialCommand>
{
    public IssueSerialValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(150);
        // SOLD added for PosStockPostingService (a POS checkout selling a specific serialized
        // unit) — reversed back to IN_STOCK by ReverseIssueSerialCommand exactly like IN_TRANSIT/
        // DISPOSED already are, no special-casing needed there.
        RuleFor(x => x.Disposition).Must(d => d is "IN_TRANSIT" or "DISPOSED" or "SOLD")
            .WithMessage("Disposition must be IN_TRANSIT, DISPOSED, or SOLD.");
    }
}
