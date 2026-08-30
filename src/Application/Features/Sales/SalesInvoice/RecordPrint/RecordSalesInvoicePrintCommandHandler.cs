namespace ZARI.Application.Features.Sales.SalesInvoices.RecordPrint;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Fired every time the receipt print view is actually triggered (the "Print Receipt" button click,
/// not just opening the detail page) — increments the BIR-audit print counter and reports back
/// whether this is a reprint, so the printed copy itself can render a "REPRINT" watermark. Only
/// requires View permission (printing a document you can already see isn't a mutation of its
/// business data), and only makes sense once the invoice actually has a BIR-OR number assigned.
/// </summary>
public sealed class RecordSalesInvoicePrintCommandHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService)
    : ICommandHandler<RecordSalesInvoicePrintCommand, Result<RecordSalesInvoicePrintResponse>>
{
    public async Task<Result<RecordSalesInvoicePrintResponse>> HandleAsync(RecordSalesInvoicePrintCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
        if (invoice is null)
            return Result.Failure<RecordSalesInvoicePrintResponse>(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.View, invoice.BranchId, cancellationToken))
            return Result.Failure<RecordSalesInvoicePrintResponse>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to view sales invoices for this branch."));

        if (invoice.BirOrSeriesNumber is null)
            return Result.Failure<RecordSalesInvoicePrintResponse>(Error.Validation("SalesInvoice.NotPosted", "This invoice has no BIR OR/SI number assigned yet — only a posted invoice can be printed as an official receipt."));

        var isReprint = invoice.PrintCount > 0;
        var now = DateTimeOffset.UtcNow;
        invoice.PrintCount += 1;
        invoice.FirstPrintedAt ??= now;
        invoice.LastPrintedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RecordSalesInvoicePrintResponse(invoice.Id, invoice.PrintCount, isReprint, invoice.FirstPrintedAt!.Value, invoice.LastPrintedAt!.Value));
    }
}
