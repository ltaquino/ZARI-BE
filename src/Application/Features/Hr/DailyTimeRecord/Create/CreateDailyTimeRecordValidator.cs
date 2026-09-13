namespace ZARI.Application.Features.Hr.DailyTimeRecords.Create;

using FluentValidation;

public sealed class CreateDailyTimeRecordValidator : AbstractValidator<CreateDailyTimeRecordCommand>
{
    private static readonly string[] ValidSources = ["MANUAL", "KIOSK", "BIOMETRIC_IMPORT"];

    public CreateDailyTimeRecordValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Source)
            .NotEmpty()
            .Must(s => ValidSources.Contains(s))
            .WithMessage($"Source must be one of: {string.Join(", ", ValidSources)}.");

        RuleFor(x => x.TimeOut)
            .GreaterThan(x => x.TimeIn!.Value)
            .When(x => x.TimeIn is not null && x.TimeOut is not null)
            .WithMessage("Time out must be after time in.");
    }
}
