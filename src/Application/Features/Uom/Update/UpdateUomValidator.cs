namespace ZARI.Application.Features.Uoms.Update;

using FluentValidation;

public sealed class UpdateUomValidator : AbstractValidator<UpdateUomCommand>
{
    public UpdateUomValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
