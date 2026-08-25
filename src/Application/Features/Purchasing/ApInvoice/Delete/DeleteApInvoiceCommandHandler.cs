namespace ZARI.Application.Features.Purchasing.ApInvoices.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteApInvoiceCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteApInvoiceCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteApInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices.FindAsync([command.Id], cancellationToken);
        if (invoice is null)
            return Result.Failure(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.Delete, invoice.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to delete AP invoices for this branch."));

        if (invoice.Status != "DRAFT")
            return Result.Failure(Error.Validation("ApInvoice.NotDraft", "Only draft AP invoices can be deleted — cancel it instead."));

        dbContext.ApInvoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
