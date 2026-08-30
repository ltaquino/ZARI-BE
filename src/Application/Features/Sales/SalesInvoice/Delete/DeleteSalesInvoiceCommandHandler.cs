namespace ZARI.Application.Features.Sales.SalesInvoices.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteSalesInvoiceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteSalesInvoiceCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteSalesInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices.FindAsync([command.Id], cancellationToken);
        if (invoice is null)
            return Result.Failure(Error.NotFound("SalesInvoice.NotFound", $"Sales invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_INVOICES", FormAction.Delete, invoice.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to delete sales invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure(Error.Validation("SalesInvoice.NotDraft", "Only draft sales invoices can be deleted — cancel it instead."));

        dbContext.SalesInvoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
