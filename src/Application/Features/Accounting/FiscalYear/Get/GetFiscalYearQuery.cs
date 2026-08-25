namespace ZARI.Application.Features.Accounting.FiscalYears.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetFiscalYearQuery(Guid Id) : IQuery<Result<FiscalYearResponse>>;

public sealed record FiscalYearResponse(Guid Id, string YearName, DateTimeOffset StartDate, DateTimeOffset EndDate, string Status, DateTimeOffset CreatedAt);
