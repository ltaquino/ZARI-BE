namespace ZARI.Application.Features.Sales.PosPromoSlides.Create;

using FluentValidation;

public sealed class CreatePosPromoSlideValidator : AbstractValidator<CreatePosPromoSlideCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePosPromoSlideValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.ImageUrl).MaximumLength(300);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
