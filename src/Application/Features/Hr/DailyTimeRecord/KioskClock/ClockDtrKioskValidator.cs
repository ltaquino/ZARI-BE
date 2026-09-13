namespace ZARI.Application.Features.Hr.DailyTimeRecords.KioskClock;

using FluentValidation;

public sealed class ClockDtrKioskValidator : AbstractValidator<ClockDtrKioskCommand>
{
    public ClockDtrKioskValidator()
    {
        RuleFor(x => x.ScannedCode).NotEmpty();
    }
}
