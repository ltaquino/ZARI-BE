namespace ZARI.Application.Features.Purchasing.ApInvoices.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Create;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateApInvoiceCommand(
    Guid Id,
    string SupplierInvoiceNo,
    DateTimeOffset InvoiceDate,
    DateTimeOffset? DueDate,
    string? Remarks,
    string? UpdatedBy,
    List<ApInvoiceLineInput> Lines) : ICommand<Result<ApInvoiceResponse>>;
