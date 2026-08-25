namespace ZARI.Application.Features.Accounting.TaxCodes.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.TaxCodes.Get;
using ZARI.Domain.Common;

public sealed record GetAllTaxCodesQuery : IQuery<Result<List<TaxCodeResponse>>>;
