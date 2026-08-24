namespace ZARI.Application.Features.Inventory.SerialNumbers.Issue;

using FluentValidation;

public sealed class IssueSerialValidator : AbstractValidator<IssueSerialCommand>
{
    public IssueSerialValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SerialNo).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Disposition).Must(d => d is "IN_TRANSIT" or "DISPOSED")
            .WithMessage("Disposition must be IN_TRANSIT or DISPOSED.");
    }
}
