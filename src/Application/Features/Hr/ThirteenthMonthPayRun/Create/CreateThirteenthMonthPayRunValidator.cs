namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Create;

using FluentValidation;

public sealed class CreateThirteenthMonthPayRunValidator : AbstractValidator<CreateThirteenthMonthPayRunCommand>
{
    public CreateThirteenthMonthPayRunValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
    }
}
