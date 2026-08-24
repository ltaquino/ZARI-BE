namespace ZARI.Application.Features.SystemModule.Branches.Update;

using FluentValidation;

public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateBranchValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.City).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
