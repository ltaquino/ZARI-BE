namespace ZARI.Application.Features.Accounting.FiscalYears.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteFiscalYearCommand(Guid Id) : ICommand;
