namespace ZARI.Application.Features.Accounting.FiscalYears.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Domain.Common;

public sealed record CreateFiscalYearCommand(string YearName, DateTimeOffset StartDate, DateTimeOffset EndDate, string Status) : ICommand<Result<FiscalYearResponse>>;
