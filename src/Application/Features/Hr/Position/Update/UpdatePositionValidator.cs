namespace ZARI.Application.Features.Hr.Positions.Update;

using FluentValidation;

public sealed class UpdatePositionValidator : AbstractValidator<UpdatePositionCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdatePositionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
