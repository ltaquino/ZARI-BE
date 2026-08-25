namespace ZARI.Application.Features.Purchasing.ApInvoices.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record GetApInvoiceQuery(Guid Id) : IQuery<Result<ApInvoiceResponse>>;
