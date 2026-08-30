namespace ZARI.Application.Features.Sales.SalesInvoices.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record GetSalesInvoiceQuery(Guid Id) : IQuery<Result<SalesInvoiceResponse>>;
