namespace ZARI.Application.Features.Purchasing.ApInvoices.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Domain.Common;

public sealed class GetApInvoiceQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetApInvoiceQuery, Result<ApInvoiceResponse>>
{
    public async Task<Result<ApInvoiceResponse>> HandleAsync(GetApInvoiceQuery query, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.ApInvoices
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .FirstOrDefaultAsync(i => i.Id == query.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<ApInvoiceResponse>(Error.NotFound("ApInvoice.NotFound", $"AP invoice with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("AP_INVOICES", FormAction.View, invoice.BranchId, cancellationToken))
            return Result.Failure<ApInvoiceResponse>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to view AP invoices for this branch."));

        var amountPaid = await ApInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
        return Result.Success(ApInvoiceMapper.ToResponse(invoice, amountPaid));
    }
}
