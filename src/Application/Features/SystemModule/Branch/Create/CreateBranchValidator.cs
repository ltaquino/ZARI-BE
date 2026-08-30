namespace ZARI.Application.Features.SystemModule.Branches.Create;

using FluentValidation;

public sealed class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreateBranchValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.City).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.BirBranchCode).MaximumLength(25);
        RuleFor(x => x.PosPermitNumber).MaximumLength(25);
        RuleFor(x => x.MachineIdentificationNumber).MaximumLength(25);
        RuleFor(x => x.MachineSerialNumber).MaximumLength(25);
    }
}
