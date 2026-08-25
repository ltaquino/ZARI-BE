namespace ZARI.Application.Features.Accounting.FiscalYears.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Domain.Common;

public sealed record GetAllFiscalYearsQuery : IQuery<Result<List<FiscalYearResponse>>>;
