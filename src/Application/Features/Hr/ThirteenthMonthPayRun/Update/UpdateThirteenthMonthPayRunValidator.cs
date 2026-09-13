namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Update;

using FluentValidation;

public sealed class UpdateThirteenthMonthPayRunValidator : AbstractValidator<UpdateThirteenthMonthPayRunCommand>
{
    public UpdateThirteenthMonthPayRunValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
