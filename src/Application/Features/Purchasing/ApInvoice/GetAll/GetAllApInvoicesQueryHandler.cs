namespace ZARI.Application.Features.Purchasing.ApInvoices.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Domain.Common;

public sealed class GetAllApInvoicesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllApInvoicesQuery, Result<List<ApInvoiceResponse>>>
{
    public async Task<Result<List<ApInvoiceResponse>>> HandleAsync(GetAllApInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("AP_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<List<ApInvoiceResponse>>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to view AP invoices."));

        var invoices = await dbContext.ApInvoices.AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var amountsPaid = await ApInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), cancellationToken);
        return Result.Success(invoices.Select(i => ApInvoiceMapper.ToResponse(i, amountsPaid[i.Id])).ToList());
    }
}
