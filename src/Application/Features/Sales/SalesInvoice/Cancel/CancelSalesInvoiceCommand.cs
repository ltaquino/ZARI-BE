namespace ZARI.Application.Features.Sales.SalesInvoices.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record CancelSalesInvoiceCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<SalesInvoiceResponse>>;
