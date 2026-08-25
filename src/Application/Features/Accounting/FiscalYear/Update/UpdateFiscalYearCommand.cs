namespace ZARI.Application.Features.Accounting.FiscalYears.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateFiscalYearCommand(Guid Id, string YearName, DateTimeOffset StartDate, DateTimeOffset EndDate, string Status) : ICommand;
