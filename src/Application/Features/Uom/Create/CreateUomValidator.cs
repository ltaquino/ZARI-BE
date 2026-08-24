namespace ZARI.Application.Features.Uoms.Create;

using FluentValidation;

public sealed class CreateUomValidator : AbstractValidator<CreateUomCommand>
{
    public CreateUomValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
