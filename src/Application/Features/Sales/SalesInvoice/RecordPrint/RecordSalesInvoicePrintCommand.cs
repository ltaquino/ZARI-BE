namespace ZARI.Application.Features.Sales.SalesInvoices.RecordPrint;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record RecordSalesInvoicePrintCommand(Guid Id, string? PrintedBy) : ICommand<Result<RecordSalesInvoicePrintResponse>>;

public sealed record RecordSalesInvoicePrintResponse(
    Guid Id,
    int PrintCount,
    bool IsReprint,
    DateTimeOffset FirstPrintedAt,
    DateTimeOffset LastPrintedAt);
