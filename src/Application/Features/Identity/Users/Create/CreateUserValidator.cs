namespace ZARI.Application.Features.Identity.Users.Create;

using FluentValidation;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleForEach(x => x.RoleIds).NotEmpty();
        RuleForEach(x => x.BranchIds).NotEmpty();
    }
}
