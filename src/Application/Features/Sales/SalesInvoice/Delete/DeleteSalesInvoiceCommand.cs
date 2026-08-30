namespace ZARI.Application.Features.Sales.SalesInvoices.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteSalesInvoiceCommand(Guid Id) : ICommand<Result>;
