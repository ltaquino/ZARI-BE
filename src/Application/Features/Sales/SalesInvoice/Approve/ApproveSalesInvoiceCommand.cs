namespace ZARI.Application.Features.Sales.SalesInvoices.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveSalesInvoiceCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<SalesInvoiceResponse>>;
