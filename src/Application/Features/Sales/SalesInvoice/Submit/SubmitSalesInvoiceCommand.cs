namespace ZARI.Application.Features.Sales.SalesInvoices.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitSalesInvoiceCommand(Guid Id, string RequestedBy) : ICommand<Result<SalesInvoiceResponse>>;
