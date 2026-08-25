namespace ZARI.Application.Features.Identity.Users.Permissions.SetOverrides;

using FluentValidation;

public sealed class SetUserPermissionOverridesValidator : AbstractValidator<SetUserPermissionOverridesCommand>
{
    public SetUserPermissionOverridesValidator()
    {
        RuleForEach(x => x.Overrides).ChildRules(o => o.RuleFor(x => x.FormCode).NotEmpty());
        RuleFor(x => x.Overrides)
            .Must(o => o.Select(x => x.FormCode).Distinct().Count() == o.Count)
            .WithMessage("Duplicate form codes are not allowed.");
    }
}
