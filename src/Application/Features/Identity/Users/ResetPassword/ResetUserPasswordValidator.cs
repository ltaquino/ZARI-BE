namespace ZARI.Application.Features.Identity.Users.ResetPassword;

using FluentValidation;

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        // Matches CreateUserValidator's own password rule -- the real floor (digit+lower+upper) is
        // still enforced by Identity.Password policy inside the handler via ChangePasswordAsync.
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
