namespace ZARI.Application.Features.Identity.Roles.Create;

using FluentValidation;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleForEach(x => x.Permissions).ChildRules(p => p.RuleFor(x => x.FormCode).NotEmpty());
        RuleFor(x => x.Permissions)
            .Must(p => p.Select(x => x.FormCode).Distinct().Count() == p.Count)
            .WithMessage("Duplicate form codes are not allowed.");
    }
}
