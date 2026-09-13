namespace ZARI.Application.Features.Hr.Positions.Create;

using FluentValidation;

public sealed class CreatePositionValidator : AbstractValidator<CreatePositionCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public CreatePositionValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DepartmentId).NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.PayGradeMax)
            .GreaterThanOrEqualTo(x => x.PayGradeMin!.Value)
            .When(x => x.PayGradeMin is not null && x.PayGradeMax is not null)
            .WithMessage("Pay grade maximum must be greater than or equal to the minimum.");
    }
}
