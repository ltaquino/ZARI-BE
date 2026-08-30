namespace ZARI.Application.Features.Sales.SalesInvoices.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record RequestSalesInvoiceCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<SalesInvoiceResponse>>;
